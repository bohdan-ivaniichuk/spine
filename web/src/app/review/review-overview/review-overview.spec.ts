import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTree } from '../../knowledge/api/knowledge-tree.models';
import { Coverage, Session } from '../api/review.models';
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

const coverage: Coverage = {
  topics: [{ domain: 'dotnet', topic: 'clr-memory', eligible: 28, seen: 12, due: 2 }],
};

describe('ReviewOverview', () => {
  let fixture: ComponentFixture<ReviewOverview>;
  let http: HttpTestingController;
  let store: SessionStore;

  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');
  let navigate: ReturnType<typeof vi.spyOn>;
  let queryParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
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
    http.expectOne('/api/review/coverage').flush(coverage);
    await settle(fixture);
  }

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    queryParams = new BehaviorSubject(convertToParamMap({}));
    await TestBed.configureTestingModule({
      imports: [ReviewOverview],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useFactory: () => ({ queryParamMap: queryParams.asObservable() }),
        },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    store = TestBed.inject(SessionStore);
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('renders "28 new · 0 due" and prefills the limits from the server defaults', async () => {
    await open(overview({ defaults: { totalLimit: 15, newLimit: 5 } }));

    expect(text()).toContain('28 new questions');
    expect(text()).toContain('0 due for review');
    expect(startButton().textContent).toContain('Start · 15 cards');
    expect(numberInput('totalLimit').value).toBe('15');
    expect(numberInput('newLimit').value).toBe('5');
    expect(startButton().disabled).toBe(false);
  });

  it('shows the empty state with the next due date and disables Start when nothing is due', async () => {
    await open(overview({ dueCount: 0, newCount: 0, nextDueAt: '2026-09-07T12:00:00+00:00' }));

    expect(text()).toContain('Nothing to review right now');
    expect(text()).toMatch(/7 Sept?/);
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
    http.expectNone('/api/review/coverage');
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
    expect(text()).toContain('4 new questions');
    expect(text()).toContain('3 due for review');
  });

  it('does not show "Starting…" while only the resume probe is pending', async () => {
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http.expectOne('/api/review/overview?scope=all').flush(overview());
    http.expectOne('/api/review/coverage').flush(coverage);
    await settle(fixture);

    expect(store.status()).toBe('loading');
    expect(startButton().textContent).toContain('Start ·');
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
    http.expectOne('/api/review/coverage').flush(null, { status: 0, statusText: 'Unknown' });
    await settle(fixture);

    expect(text()).toContain('API unreachable');
    expect(startButton().disabled).toBe(true);

    root().querySelector<HTMLButtonElement>('button[type=button]')!.click();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http.expectOne('/api/review/overview?scope=all').flush(overview());
    http.expectOne('/api/review/coverage').flush(coverage);
    await settle(fixture);
    expect(text()).toContain('28 new questions');
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

  it('draws the atlas: numbered domain tiles, topic coverage and "soon" for empty topics', async () => {
    await open();

    const tiles = Array.from(root().querySelectorAll('article'));
    expect(tiles).toHaveLength(2);
    expect(tiles[0].textContent).toContain('01');
    expect(tiles[0].querySelector('button')?.className).toContain('domain-1');
    expect(tiles[0].textContent).toContain('12/28');
    expect(tiles[0].textContent).toContain('2 due');
    expect(tiles[0].textContent).not.toContain('Old topic');
    expect(tiles[1].textContent).toContain('02');
    expect(tiles[1].textContent).toContain('content in progress');
  });

  it('picks a topic from the atlas and asks for its counts', async () => {
    await open();

    Array.from(root().querySelectorAll<HTMLButtonElement>('article li button'))
      .find((b) => b.textContent?.includes('Memory & GC'))!
      .click();
    TestBed.tick();
    http
      .expectOne('/api/review/overview?scope=topic&domain=dotnet&topic=clr-memory')
      .flush(overview({ dueCount: 2, newCount: 16, totalEligible: 28 }));
    await settle(fixture);

    expect(select('scopeType').value).toBe('topic');
    expect(select('topic').value).toBe('clr-memory');
    expect(text()).toContain('16 new questions');
    // The choice is written to the URL, so the spine tab for this domain is a real navigation.
    expect(navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: { scope: 'topic', domain: 'dotnet', topic: 'clr-memory' },
        replaceUrl: true,
      }),
    );
  });

  it('presets the scope from ?scope=domain&domain= (the spine tabs)', async () => {
    queryParams.next(convertToParamMap({ scope: 'domain', domain: 'dotnet' }));
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(tree);
    http
      .expectOne('/api/review/sessions/current')
      .flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/review/overview?scope=domain&domain=dotnet').flush(overview());
    http.expectOne('/api/review/coverage').flush(coverage);
    await settle(fixture);

    expect(select('scopeType').value).toBe('domain');
    expect(select('domain').value).toBe('dotnet');

    // Back to "all" in the URL (e.g. browser back) resets the picker.
    queryParams.next(convertToParamMap({}));
    TestBed.tick();
    http.expectOne('/api/review/overview?scope=all').flush(overview());
    await settle(fixture);
    expect(select('scopeType').value).toBe('all');
  });

  it('speaks Ukrainian: labels, plurals and ?lang=uk on every request', async () => {
    TestBed.inject(I18n).set('uk');
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree?lang=uk').flush(tree);
    http
      .expectOne('/api/review/sessions/current?lang=uk')
      .flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/review/overview?scope=all&lang=uk').flush(overview({ newCount: 25 }));
    http.expectOne('/api/review/coverage?lang=uk').flush(coverage);
    await settle(fixture);

    expect(text()).toContain('Сьогодні');
    expect(text()).toContain('25 нових питань');
    expect(startButton().textContent).toContain('Почати · 20 карток');
    TestBed.inject(I18n).set('en');
  });
});
