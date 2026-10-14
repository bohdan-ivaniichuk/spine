import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { SessionStore } from '../session-store';
import { feedback, question, session, settle, summary } from '../testing/fixtures';
import { ReviewSession } from './review-session';

describe('ReviewSession', () => {
  let fixture: ComponentFixture<ReviewSession>;
  let http: HttpTestingController;
  let store: SessionStore;
  let navigate: ReturnType<typeof vi.spyOn>;

  const SESSIONS = '/api/review/sessions';
  const ID = session().id;

  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => root().textContent ?? '';
  const radios = () => Array.from(root().querySelectorAll<HTMLButtonElement>('button[role=radio]'));
  const button = (label: string) =>
    Array.from(root().querySelectorAll<HTMLButtonElement>('button')).find((b) =>
      b.textContent?.trim().startsWith(label),
    );

  const press = (key: string, target: EventTarget = document) => {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    target.dispatchEvent(event);
    return event;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewSession],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  /** Creates the page, answers the resume request with a session and waits for the first render. */
  async function open(count = 2) {
    fixture = TestBed.createComponent(ReviewSession);
    fixture.detectChanges();
    http.expectOne(`${SESSIONS}/current`).flush(session(count));
    await settle(fixture);
  }

  it('redirects to the overview when there is no open session', async () => {
    fixture = TestBed.createComponent(ReviewSession);
    fixture.detectChanges();
    http.expectOne(`${SESSIONS}/current`).flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);

    expect(navigate).toHaveBeenCalledWith('/review');
  });

  it('resumes the open session and renders the first question with progress and scope', async () => {
    await open(2);

    expect(text()).toContain('Question 1');
    expect(text()).toContain('1 / 2');
    expect(text()).toContain('C# & .NET Runtime › Memory & GC');
    expect(radios()).toHaveLength(4);
    expect(button('Check')?.disabled).toBe(true);
  });

  it('key 2 selects the second option, Enter checks, Enter again advances', async () => {
    await open(2);

    press('2');
    await settle(fixture);
    expect(radios()[1].getAttribute('aria-checked')).toBe('true');
    expect(button('Check')?.disabled).toBe(false);

    const enter = press('Enter');
    expect(enter.defaultPrevented).toBe(true);
    const req = http.expectOne(`${SESSIONS}/${ID}/answers`);
    expect(req.request.body).toEqual({ questionId: question(1).id, selectedIndex: 1 });
    req.flush(feedback(question(1).id, 1, false));
    await settle(fixture);

    expect(text()).toContain('Incorrect');
    expect(text()).toContain('allocates on the stack');
    expect(radios()[0].dataset['state']).toBe('correct');
    expect(radios()[1].dataset['state']).toBe('wrong');
    expect(radios().every((r) => r.disabled)).toBe(true);
    expect(button('Next')).toBeDefined();

    press('Enter');
    await settle(fixture);

    expect(text()).toContain('Question 2');
    expect(text()).toContain('2 / 2');
    expect(radios().every((r) => r.getAttribute('aria-checked') === 'false')).toBe(true);
    expect(button('Finish')).toBeUndefined();
    expect(button('Check')).toBeDefined();
  });

  it('shows Finish on the last question and opens the summary after it', async () => {
    await open(1);

    press('a');
    press('Enter');
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);

    expect(text()).toContain('Correct');
    expect(button('Finish')).toBeDefined();

    button('Finish')!.click();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary());
    await settle(fixture);

    expect(store.summary()?.correct).toBe(15);
    expect(navigate).toHaveBeenLastCalledWith('/review/summary');
  });

  it('moves focus to the feedback after Check', async () => {
    await open(2);

    press('1');
    press('Enter');
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);

    const panel = root().querySelector<HTMLElement>('app-feedback-panel')!;
    expect(panel.getAttribute('tabindex')).toBe('-1');
    expect(document.activeElement).toBe(panel);
  });

  it('moves focus to the new question card after Next', async () => {
    await open(2);

    press('1');
    press('Enter');
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);
    press('Enter');
    await settle(fixture);

    const card = root().querySelector<HTMLElement>('article')!;
    expect(card.getAttribute('tabindex')).toBe('-1');
    expect(card.getAttribute('aria-label')).toBe('Question 2 of 2');
    expect(document.activeElement).toBe(card);
  });

  it('leaves Enter to a focused link or button instead of checking or advancing', async () => {
    await open(2);
    press('1');
    await settle(fixture);

    const end = button('End session')!;
    end.focus();
    const onButton = press('Enter', end);
    expect(onButton.defaultPrevented).toBe(false);
    http.expectNone(`${SESSIONS}/${ID}/answers`);

    const other = radios()[2];
    other.focus();
    const onRadio = press('Enter', other);
    expect(onRadio.defaultPrevented).toBe(false);
    http.expectNone(`${SESSIONS}/${ID}/answers`);

    const checked = radios()[0];
    checked.focus();
    const onChecked = press('Enter', checked);
    expect(onChecked.defaultPrevented).toBe(true);
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);
    expect(text()).toContain('Correct');
  });

  it('keeps the card, shows the error and lets Finish be retried when finishing fails', async () => {
    await open(1);

    press('1');
    press('Enter');
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);

    button('Finish')!.click();
    http
      .expectOne(`${SESSIONS}/${ID}/finish`)
      .flush({ title: 'Service unavailable' }, { status: 503, statusText: 'Service Unavailable' });
    await settle(fixture);

    expect(text()).toContain('Question 1');
    expect(text()).toContain('Correct');
    expect(root().querySelector('[role=alert]')?.textContent).toContain('Service unavailable');
    expect(button('Finish')?.disabled).toBe(false);

    button('Finish')!.click();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary());
    await settle(fixture);
    expect(navigate).toHaveBeenLastCalledWith('/review/summary');
  });

  it('offers a retry when a fully answered session cannot be finished on resume', async () => {
    fixture = TestBed.createComponent(ReviewSession);
    fixture.detectChanges();
    http.expectOne(`${SESSIONS}/current`).flush(
      session(1, {
        answers: [{ questionId: question(1).id, selectedIndex: 0, isCorrect: true }],
      }),
    );
    await settle(fixture);
    http
      .expectOne(`${SESSIONS}/${ID}/finish`)
      .flush({ title: 'Service unavailable' }, { status: 503, statusText: 'Service Unavailable' });
    await settle(fixture);

    expect(text()).toContain('Could not finish the session');
    expect(root().querySelector('[role=alert]')?.textContent).toContain('Service unavailable');

    button('Retry')!.click();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary());
    await settle(fixture);
    expect(navigate).toHaveBeenLastCalledWith('/review/summary');
  });

  it('ignores keys typed inside an input and keys with modifiers', async () => {
    await open(1);
    const input = document.createElement('input');
    document.body.appendChild(input);
    try {
      press('1', input);
      await settle(fixture);
      expect(store.selectedIndex()).toBeNull();

      document.dispatchEvent(
        new KeyboardEvent('keydown', { key: '1', ctrlKey: true, bubbles: true }),
      );
      await settle(fixture);
      expect(store.selectedIndex()).toBeNull();
    } finally {
      input.remove();
    }
  });

  it('ignores digits after the answer is checked and Enter without a selection', async () => {
    await open(2);

    const enter = press('Enter');
    expect(enter.defaultPrevented).toBe(false);
    http.expectNone(`${SESSIONS}/${ID}/answers`);

    press('1');
    press('Enter');
    http.expectOne(`${SESSIONS}/${ID}/answers`).flush(feedback(question(1).id, 0, true));
    await settle(fixture);

    press('3');
    await settle(fixture);
    expect(store.selectedIndex()).toBe(0);
  });

  it('keeps the selection and shows the error when the answer request fails', async () => {
    await open(1);

    radios()[2].click();
    await settle(fixture);
    button('Check')!.click();
    http
      .expectOne(`${SESSIONS}/${ID}/answers`)
      .flush({ title: 'Service unavailable' }, { status: 503, statusText: 'Service Unavailable' });
    await settle(fixture);

    expect(root().querySelector('[role=alert]')?.textContent).toContain('Service unavailable');
    expect(radios()[2].getAttribute('aria-checked')).toBe('true');
    expect(button('Check')?.disabled).toBe(false);
  });

  it('End session asks for confirmation and finishes early', async () => {
    await open(3);
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    button('End session')!.click();
    http.expectOne(`${SESSIONS}/${ID}/finish`).flush(summary({ answered: 0, correct: 0 }));
    await settle(fixture);

    expect(window.confirm).toHaveBeenCalled();
    expect(navigate).toHaveBeenLastCalledWith('/review/summary');
  });
});
