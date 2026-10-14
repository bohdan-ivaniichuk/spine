import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { SessionStore } from './session-store';
import { feedback, question, session, summary } from './testing/fixtures';

describe('SessionStore', () => {
  let store: SessionStore;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  const SESSIONS = '/api/review/sessions';
  const ID = session().id;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    store = TestBed.inject(SessionStore);
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  /** Starts a fresh session through the API and returns once the store is ready. */
  async function startSession(count = 3) {
    const pending = store.start({ scope: 'topic', domain: 'dotnet', topic: 'clr-memory' });
    http.expectOne(SESSIONS).flush(session(count), { status: 201, statusText: 'Created' });
    await pending;
  }

  async function checkAnswer(selectedIndex: number, isCorrect: boolean) {
    store.select(selectedIndex);
    const pending = store.check();
    const req = http.expectOne(`${SESSIONS}/${ID}/answers`);
    req.flush(feedback(store.current()!.id, selectedIndex, isCorrect));
    await pending;
    return req;
  }

  it('start posts the request, stores the session and navigates to the card page', async () => {
    const pending = store.start({
      scope: 'topic',
      domain: 'dotnet',
      topic: 'clr-memory',
      totalLimit: 20,
      newLimit: 10,
    });
    expect(store.status()).toBe('loading');

    const req = http.expectOne(SESSIONS);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      scope: 'topic',
      domain: 'dotnet',
      topic: 'clr-memory',
      totalLimit: 20,
      newLimit: 10,
    });
    req.flush(session(3), { status: 201, statusText: 'Created' });
    await pending;

    expect(store.status()).toBe('ready');
    expect(store.session()?.id).toBe(ID);
    expect(store.total()).toBe(3);
    expect(store.position()).toBe(1);
    expect(store.current()?.id).toBe(question(1).id);
    expect(store.scopeLabel()).toBe('C# & .NET Runtime › Memory & GC');
    expect(navigate).toHaveBeenCalledWith('/review/session');
  });

  it('start surfaces a ProblemDetails error and stays on the overview', async () => {
    const pending = store.start({ scope: 'all' });
    http
      .expectOne(SESSIONS)
      .flush({ title: 'Nothing to review', status: 409 }, { status: 409, statusText: 'Conflict' });
    await pending;

    expect(store.status()).toBe('error');
    expect(store.error()).toContain('Nothing to review');
    expect(store.session()).toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('select then check posts the answer and locks the card', async () => {
    await startSession();

    store.select(2);
    expect(store.selectedIndex()).toBe(2);

    const pending = store.check();
    expect(store.status()).toBe('checking');
    const req = http.expectOne(`${SESSIONS}/${ID}/answers`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ questionId: question(1).id, selectedIndex: 2 });
    req.flush(feedback(question(1).id, 2, false));
    await pending;

    expect(store.status()).toBe('checked');
    expect(store.feedback()?.correctIndex).toBe(0);
    expect(store.answered().get(question(1).id)).toEqual({
      questionId: question(1).id,
      selectedIndex: 2,
      isCorrect: false,
    });
    expect(store.current()?.id).toBe(question(1).id);
    expect(store.position()).toBe(1);

    store.select(1);
    expect(store.selectedIndex()).toBe(2);
  });

  it('a failed check keeps the selection and reports the error so Check can be retried', async () => {
    await startSession();
    store.select(1);

    const pending = store.check();
    http
      .expectOne(`${SESSIONS}/${ID}/answers`)
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    await pending;

    expect(store.status()).toBe('ready');
    expect(store.selectedIndex()).toBe(1);
    expect(store.error()).toBeTruthy();
    expect(store.feedback()).toBeNull();

    const retry = store.check();
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 1, false));
    await retry;
    expect(store.status()).toBe('checked');
    expect(store.error()).toBeNull();
  });

  it('next advances to the following question and clears the selection', async () => {
    await startSession();
    await checkAnswer(0, true);

    store.next();

    expect(store.status()).toBe('ready');
    expect(store.position()).toBe(2);
    expect(store.current()?.id).toBe(question(2).id);
    expect(store.selectedIndex()).toBeNull();
    expect(store.feedback()).toBeNull();
    expect(store.isLast()).toBe(false);
  });

  it('next on the last question finishes the session and opens the summary', async () => {
    await startSession(1);
    await checkAnswer(0, true);
    expect(store.isLast()).toBe(true);

    store.next();
    expect(store.status()).toBe('finishing');
    const req = http.expectOne(`${SESSIONS}/${ID}/finish`);
    expect(req.request.method).toBe('POST');
    req.flush(summary());
    await vi.waitFor(() => expect(store.status()).toBe('finished'));

    expect(store.summary()?.accuracy).toBe(0.75);
    expect(store.session()).toBeNull();
    expect(store.limits()).toEqual({ totalLimit: 20, newLimit: 10 });
    expect(navigate).toHaveBeenLastCalledWith('/review/summary');
  });

  it('a failed finish after the last Next keeps the checked card so Finish can be retried', async () => {
    await startSession(1);
    await checkAnswer(0, true);

    store.next();
    http
      .expectOne(`${SESSIONS}/${ID}/finish`)
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    await vi.waitFor(() => expect(store.status()).toBe('checked'));

    expect(store.error()).toBeTruthy();
    expect(store.feedback()?.questionId).toBe(question(1).id);
    expect(store.current()?.id).toBe(question(1).id);
    expect(store.session()).not.toBeNull();

    const retry = store.finish();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary());
    await retry;
    expect(store.status()).toBe('finished');
    expect(store.feedback()).toBeNull();
  });

  it('reviewAgain starts a new session with the same scope and limits', async () => {
    await startSession(1);
    await checkAnswer(0, true);
    store.next();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary());
    await vi.waitFor(() => expect(store.status()).toBe('finished'));

    const pending = store.reviewAgain();
    const req = http.expectOne(SESSIONS);
    expect(req.request.body).toEqual({
      scope: 'topic',
      domain: 'dotnet',
      topic: 'clr-memory',
      totalLimit: 20,
      newLimit: 10,
    });
    req.flush(session(2), { status: 201, statusText: 'Created' });
    await pending;
    expect(store.status()).toBe('ready');
  });

  it('resume with 204 leaves the session empty', async () => {
    const pending = store.resume();
    http.expectOne(`${SESSIONS}/current`).flush(null, { status: 204, statusText: 'No Content' });

    expect(await pending).toBeNull();
    expect(store.session()).toBeNull();
    expect(store.status()).toBe('idle');
  });

  it('resume starts at the first unanswered question and honours skippedQuestionIds', async () => {
    const pending = store.resume();
    http.expectOne(`${SESSIONS}/current`).flush(
      session(4, {
        answers: [
          { questionId: question(1).id, selectedIndex: 0, isCorrect: true },
          { questionId: question(2).id, selectedIndex: 3, isCorrect: false },
        ],
        skippedQuestionIds: [question(3).id],
      }),
    );
    await pending;

    expect(store.status()).toBe('ready');
    expect(store.total()).toBe(3);
    expect(store.answeredCount()).toBe(2);
    expect(store.current()?.id).toBe(question(4).id);
    expect(store.position()).toBe(3);
    expect(store.isLast()).toBe(true);
  });

  it('resume reuses the session already in memory without a request', async () => {
    await startSession();
    const resumed = await store.resume();
    expect(resumed?.id).toBe(ID);
    http.expectNone(`${SESSIONS}/current`);
  });
});
