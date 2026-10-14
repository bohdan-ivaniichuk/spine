import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { SessionStore } from '../session-store';
import { feedback, question, session, summary } from '../testing/fixtures';
import { SessionSummary } from './session-summary';

describe('SessionSummary', () => {
  let fixture: ComponentFixture<SessionSummary>;
  let http: HttpTestingController;
  let store: SessionStore;
  let navigate: ReturnType<typeof vi.spyOn>;

  const SESSIONS = '/api/review/sessions';
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [SessionSummary],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  /** Drives the store through a one-question session so it holds a real summary. */
  async function finishSession(body = summary()) {
    const resume = store.resume();
    http.expectOne(`${SESSIONS}/current`).flush(session(1));
    await resume;
    store.select(0);
    const check = store.check();
    http.expectOne(`${SESSIONS}/${session().id}/answers`).flush(feedback(question(1).id, 0, true));
    await check;
    const finish = store.finish();
    http.expectOne(`${SESSIONS}/${session().id}/finish`).flush(body);
    await finish;
  }

  async function render() {
    fixture = TestBed.createComponent(SessionSummary);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('redirects to the overview when there is no summary in memory', async () => {
    await render();
    expect(navigate).toHaveBeenCalledWith('/review');
    expect(root().querySelector('h1')).toBeNull();
  });

  it('shows counts, accuracy, due tomorrow and the scope', async () => {
    await finishSession();
    await render();

    expect(text()).toContain('Session complete');
    expect(text()).toContain('C# & .NET Runtime › Memory & GC');
    expect(text()).toContain('20 / 20 answered');
    expect(text()).toContain('75%');
    expect(text()).toContain('Due tomorrow in this scope: 5');
  });

  it('lists weak concepts in order as disclosures, collapsed by default', async () => {
    await finishSession();
    await render();

    const items = Array.from(root().querySelectorAll('ol li'));
    const buttons = items.map((li) => li.querySelector('button')!);
    expect(buttons.map((b) => b.textContent?.replace(/[▸▾]/g, '').trim().split(/\s+/)[0])).toEqual([
      'GC',
      'Span<T>,',
    ]);
    expect(items[0].textContent).toContain('2 / 3 wrong');
    expect(buttons[0].getAttribute('aria-expanded')).toBe('false');
    expect(root().querySelector('a[href^="/knowledge"]')).toBeNull();
  });

  it('opens a weak concept in place with its note from the concept read endpoint', async () => {
    await finishSession();
    await render();

    const button = root().querySelector<HTMLButtonElement>('ol li button')!;
    button.click();
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/concepts/dotnet/clr-memory/gc-modes').flush({
      domain: { slug: 'dotnet', title: 'C# & .NET Runtime' },
      topic: { slug: 'clr-memory', title: 'Memory & GC' },
      slug: 'gc-modes',
      level: 'S',
      reviewed: false,
      title: 'GC modes',
      note: '## What it is\n\n**Server GC** uses one *ephemeral* heap per core; see `GCSettings` and `int*`.',
      questionCount: 5,
      kataCount: 0,
      deprecatedAt: null,
    });
    await fixture.whenStable();

    expect(button.getAttribute('aria-expanded')).toBe('true');
    expect(text()).toContain('What it is');
    expect(text()).toContain('Server GC uses one ephemeral heap per core');
    expect(text()).not.toContain('##');
    expect(Array.from(root().querySelectorAll('ol li code')).map((c) => c.textContent)).toEqual([
      'GCSettings',
      'int*',
    ]);

    button.click();
    fixture.detectChanges();
    expect(button.getAttribute('aria-expanded')).toBe('false');
    expect(text()).not.toContain('What it is');
  });

  it('shows a dash for accuracy and a friendly message when nothing was answered', async () => {
    await finishSession(summary({ answered: 0, correct: 0, accuracy: null, weakConcepts: [] }));
    await render();

    expect(text()).toContain('Accuracy —');
    expect(text()).toContain('No misses');
  });

  it('"Review again" starts a new session with the same scope and limits', async () => {
    await finishSession();
    await render();
    const start = vi.spyOn(store, 'start').mockResolvedValue();

    Array.from(root().querySelectorAll('button'))
      .find((b) => b.textContent?.includes('Review again'))!
      .click();

    expect(start).toHaveBeenCalledWith({
      scope: 'topic',
      domain: 'dotnet',
      topic: 'clr-memory',
      totalLimit: 20,
      newLimit: 10,
    });
  });
});
