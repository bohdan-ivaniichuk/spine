import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { KnowledgeTree } from '../../knowledge/api/knowledge-tree.models';
import { Session } from '../api/review.models';
import { SessionStore } from '../session-store';
import { overview, session, settle } from '../testing/fixtures';
import { ReviewOverview } from './review-overview';

const tree: KnowledgeTree = {
  language: 'en',
  domains: [
    {
      slug: 'dotnet',
      title: 'C# & .NET Runtime',
      sortOrder: 1,
      deprecatedAt: null,
      topics: [
        {
          slug: 'clr-memory',
          title: 'Memory & GC',
          sortOrder: 1,
          deprecatedAt: null,
          concepts: [],
        },
        {
          slug: 'old',
          title: 'Old topic',
          sortOrder: 2,
          deprecatedAt: '2026-01-01T00:00:00+00:00',
          concepts: [],
        },
      ],
    },
    { slug: 'angular', title: 'Angular', sortOrder: 2, deprecatedAt: null, topics: [] },
  ],
};

describe('ReviewOverview', () => {
  let fixture: ComponentFixture<ReviewOverview>;
  let http: HttpTestingController;
  let store: SessionStore;

  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => root().textContent ?? '';
  const startButton = () => root().querySelector<HTMLButtonElement>('button[type=submit]')!;
  const select = (name: string) =>
    root().querySelector<HTMLSelectElement>(`select[formControlName=${name}]`)!;
  const numberInput = (name: string) =>
    root().querySelector<HTMLInputElement>(`input[formControlName=${name}]`)!;

  /** Synchronous: a select change may open an httpResource request that the test must flush first. */
  const setSelect = (name: string, value: string) => {
    const el = select(name);
    el.value = value;
    el.dispatchEvent(new Event('change'));
    TestBed.tick();
  };

  const setNumber = async (name: string, value: string) => {
    const el = numberInput(name);
    el.value = value;
    el.dispatchEvent(new Event('input'));
    await settle(fixture);
  };

  /** Creates the page and answers the three initial requests (tree, current session, overview). */
  async function open(overviewBody = overview(), current: Session | null = null) {
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http
      .expectOne('/api/review/sessions/current')
      .flush(current, current ? undefined : { status: 204, statusText: 'No Content' });
    http.expectOne('/api/review/overview?scope=all').flush(overviewBody);
    await settle(fixture);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewOverview],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('renders "0 due · 28 new" and prefills the limits from the server defaults', async () => {
    await open(overview({ defaults: { totalLimit: 15, newLimit: 5 } }));

    expect(text().replace(/\s+/g, ' ')).toContain('0 due · 28 new');
    expect(numberInput('totalLimit').value).toBe('15');
    expect(numberInput('newLimit').value).toBe('5');
    expect(startButton().disabled).toBe(false);
  });

  it('shows the empty state with the next due date and disables Start when nothing is due', async () => {
    await open(overview({ dueCount: 0, newCount: 0, nextDueAt: '2026-09-07T12:00:00+00:00' }));

    expect(text()).toContain('Nothing to review right now');
    expect(text()).toContain('Sep 7, 2026');
    expect(startButton().disabled).toBe(true);
  });

  it('points to the import command when the scope has no content at all', async () => {
    await open(overview({ dueCount: 0, newCount: 0, totalEligible: 0 }));

    expect(text()).toContain('import-content');
    expect(startButton().disabled).toBe(true);
  });

  it('marks limits outside 1–100 invalid and disables Start', async () => {
    await open();

    await setNumber('totalLimit', '0');
    expect(text()).toContain('Enter a number from 1 to 100');
    expect(startButton().disabled).toBe(true);

    await setNumber('totalLimit', '101');
    expect(startButton().disabled).toBe(true);

    await setNumber('totalLimit', '25');
    expect(startButton().disabled).toBe(false);
  });

  it('requests topic counts once a domain and topic are chosen, hiding deprecated topics', async () => {
    await open();

    setSelect('scopeType', 'topic');
    await settle(fixture);
    expect(text()).toContain('Pick a domain and a topic');
    http.expectNone((req) => req.url.startsWith('/api/review/overview'));
    expect(select('topic').disabled).toBe(true);
    expect(fixture.componentInstance['form'].controls.topic.disabled).toBe(true);

    setSelect('domain', 'dotnet');
    await settle(fixture);
    http.expectNone((req) => req.url.startsWith('/api/review/overview'));
    expect(select('topic').disabled).toBe(false);
    expect(fixture.componentInstance['form'].controls.topic.enabled).toBe(true);
    expect(Array.from(select('topic').options).map((o) => o.value)).toEqual(['', 'clr-memory']);

    setSelect('topic', 'clr-memory');
    http
      .expectOne('/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory')
      .flush(overview({ dueCount: 3, newCount: 4, totalEligible: 12 }));
    await settle(fixture);
    expect(text().replace(/\s+/g, ' ')).toContain('3 due · 4 new');
  });

  it('does not show "Starting…" while only the resume probe is pending', async () => {
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http.expectOne('/api/review/overview?scope=all').flush(overview());
    await settle(fixture);

    expect(store.status()).toBe('loading');
    expect(startButton().textContent).toContain('Start session');
    expect(startButton().disabled).toBe(false);

    http
      .expectOne('/api/review/sessions/current')
      .flush(null, { status: 204, statusText: 'No Content' });
    await settle(fixture);
  });

  it('starts a session with the chosen scope and limits', async () => {
    await open();
    const start = vi.spyOn(store, 'start').mockResolvedValue();

    await setNumber('totalLimit', '12');
    await setNumber('newLimit', '4');
    startButton().click();

    expect(start).toHaveBeenCalledWith({ scope: 'all', totalLimit: 12, newLimit: 4 });
  });

  it('shows the API-unreachable state with a retry that re-requests', async () => {
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(null, { status: 0, statusText: 'Unknown' });
    http
      .expectOne('/api/review/sessions/current')
      .flush(null, { status: 0, statusText: 'Unknown' });
    http
      .expectOne('/api/review/overview?scope=all')
      .flush(null, { status: 0, statusText: 'Unknown' });
    await settle(fixture);

    expect(text()).toContain('API unreachable');
    expect(startButton().disabled).toBe(true);

    root().querySelector<HTMLButtonElement>('button[type=button]')!.click();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http.expectOne('/api/review/overview?scope=all').flush(overview());
    await settle(fixture);
    expect(text().replace(/\s+/g, ' ')).toContain('0 due · 28 new');
  });

  it('offers to resume an unfinished session', async () => {
    await open(
      overview(),
      session(3, { answers: [{ questionId: 'x', selectedIndex: 0, isCorrect: true }] }),
    );

    expect(text()).toContain('Unfinished session');
    expect(text()).toContain('C# & .NET Runtime › Memory & GC');
    const resume = Array.from(root().querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Resume'),
    )!;
    resume.click();
    expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/review/session');
  });
});
