import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTree } from '../../knowledge/api/knowledge-tree.models';
import { treeConcept } from '../../knowledge/testing/atlas-fixtures';
import { ACTIVITY_URL } from '../api/activity.models';
import { Coverage, Session } from '../api/review.models';
import { SessionStore } from '../session-store';
import { activity, overview, session, settle as settleFixture } from '../testing/fixtures';
import { ReviewOverview } from './review-overview';

const tree: KnowledgeTree = {
  language: 'en',
  domains: [
    {
      slug: 'dotnet',
      title: 'C# & .NET Runtime',
      titleFallback: false,
      sortOrder: 1,
      deprecatedAt: null,
      topics: [
        {
          slug: 'clr-memory',
          title: 'Memory & GC',
          titleFallback: false,
          sortOrder: 1,
          deprecatedAt: null,
          concepts: [],
        },
        {
          slug: 'old',
          title: 'Old topic',
          titleFallback: false,
          sortOrder: 2,
          deprecatedAt: '2026-01-01T00:00:00+00:00',
          concepts: [],
        },
      ],
    },
    {
      slug: 'angular',
      title: 'Angular',
      titleFallback: false,
      sortOrder: 2,
      deprecatedAt: null,
      topics: [],
    },
  ],
};

/** The same tree as the API serves it for ?lang=uk: Ukrainian titles where the manifest has them. */
const ukTree: KnowledgeTree = {
  ...tree,
  language: 'uk',
  domains: tree.domains.map((d) =>
    d.slug === 'dotnet'
      ? {
          ...d,
          title: 'C# та середовище виконання .NET',
          topics: d.topics.map((t) =>
            t.slug === 'clr-memory' ? { ...t, title: "Пам'ять і GC" } : t,
          ),
        }
      : { ...d, titleFallback: true },
  ),
};

/** `tree` with concepts under clr-memory (one deprecated) for the concept scope. */
const conceptTree: KnowledgeTree = {
  ...tree,
  domains: tree.domains.map((d) => ({
    ...d,
    topics: d.topics.map((t) =>
      t.slug === 'clr-memory'
        ? {
            ...t,
            concepts: [
              treeConcept('stack-vs-heap', { title: 'Stack vs heap', sortOrder: 1 }),
              treeConcept('retired', { sortOrder: 2, deprecatedAt: '2026-01-01T00:00:00+00:00' }),
              treeConcept('gc-modes', { title: 'GC modes', sortOrder: 3 }),
            ],
          }
        : t,
    ),
  })),
};

const coverage: Coverage = {
  topics: [{ domain: 'dotnet', topic: 'clr-memory', eligible: 28, seen: 12, due: 2 }],
};

describe('ReviewOverview', () => {
  let fixture: ComponentFixture<ReviewOverview>;
  /** Activity requests answered so far; the hosted card (feature 007) has its own spec. */
  let activityLoads = 0;

  /** Answers the hosted activity card's request, then settles as usual. */
  async function settle(f: ComponentFixture<unknown>) {
    TestBed.tick(); // let a card rendered in this tick raise its request before it is answered
    for (const r of http.match((req) => req.url.startsWith(ACTIVITY_URL))) {
      r.flush(activity());
      activityLoads++;
    }
    await settleFixture(f);
  }
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
    activityLoads = 0;
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

  it('hosts the activity card and anchors the start form for its empty state', async () => {
    await open();

    expect(fixture.nativeElement.querySelector('app-activity-card')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('form#review-start')).not.toBeNull();
    expect(activityLoads).toBe(1);
  });

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

  it("drops a refused start's alert once the scope changes", async () => {
    await open();

    startButton().click();
    http
      .expectOne('/api/review/sessions')
      .flush({ title: 'Nothing to review', status: 409 }, { status: 409, statusText: 'Conflict' });
    await settle(fixture);
    expect(root().querySelector('[role=alert]')?.textContent).toContain('Nothing to review');

    setSelect('scopeType', 'domain');
    setSelect('domain', 'dotnet');
    http.expectOne('/api/review/overview?scope=domain&domain=dotnet').flush(overview());
    await settle(fixture);
    expect(root().querySelector('[role=alert]')).toBeNull();
  });

  it('clears a refused start before resuming the open session', async () => {
    await open(overview(), session(2));
    const clear = vi.spyOn(store, 'clearError');

    Array.from(root().querySelectorAll('button'))
      .find((b) => b.textContent?.includes('Resume'))!
      .click();

    expect(clear).toHaveBeenCalled();
  });

  it('starts a session with the chosen scope and limits', async () => {
    await open();
    const start = vi.spyOn(store, 'start').mockResolvedValue(true);

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

    Array.from(root().querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.trim() === 'Retry')!
      .click();
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

  it('re-reads domain and topic titles from the API when the language switches', async () => {
    await open();
    expect(root().querySelectorAll('article')[0].textContent).toContain('C# & .NET Runtime');

    TestBed.inject(I18n).set('uk');
    TestBed.tick();
    http.expectOne('/api/knowledge/tree?lang=uk').flush(ukTree);
    http.expectOne('/api/review/overview?scope=all&lang=uk').flush(overview());
    http.expectOne('/api/review/coverage?lang=uk').flush(coverage);
    await settle(fixture);

    const tiles = Array.from(root().querySelectorAll('article'));
    expect(tiles[0].textContent).toContain('C# та середовище виконання .NET');
    expect(tiles[0].textContent).toContain("Пам'ять і GC");
    expect(tiles[0].textContent).not.toContain('C# & .NET Runtime');
    expect(tiles[0].querySelector('[lang=en]')).toBeNull();
    // `angular` has no Ukrainian title: it falls back to English with the EN badge and lang="en".
    expect(tiles[1].querySelector('[lang=en]')?.textContent).toContain('Angular');
    expect(tiles[1].querySelector('app-en-badge')?.textContent).toContain('EN');
    expect(tiles[0].querySelector('app-en-badge')?.textContent?.trim() ?? '').toBe('');
    // Domain scope without a picked domain issues no overview request (afterEach verifies).
    setSelect('scopeType', 'domain');
    await settle(fixture);
    expect(Array.from(select('domain').options).map((o) => o.textContent?.trim())).toContain(
      'C# та середовище виконання .NET',
    );
    TestBed.inject(I18n).set('en');
  });

  it('marks fallback topic titles in the atlas tiles with the EN badge under Ukrainian', async () => {
    TestBed.inject(I18n).set('uk');
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree?lang=uk').flush({
      ...ukTree,
      domains: ukTree.domains.map((d) => ({
        ...d,
        topics: d.topics.map((t) => ({ ...t, title: 'Memory & GC', titleFallback: true })),
      })),
    });
    http
      .expectOne('/api/review/sessions/current?lang=uk')
      .flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/review/overview?scope=all&lang=uk').flush(overview());
    http.expectOne('/api/review/coverage?lang=uk').flush(coverage);
    await settle(fixture);

    const topic = root().querySelector('article li button')!;
    expect(topic.querySelector('[lang=en]')?.textContent).toContain('Memory & GC');
    expect(topic.querySelector('app-en-badge')?.textContent).toContain('EN');
    TestBed.inject(I18n).set('en');
  });

  it('presets ?scope=concept&domain=&topic=&concept= and the concept select follows the topic', async () => {
    queryParams.next(
      convertToParamMap({
        scope: 'concept',
        domain: 'dotnet',
        topic: 'clr-memory',
        concept: 'gc-modes',
      }),
    );
    fixture = TestBed.createComponent(ReviewOverview);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(conceptTree);
    http
      .expectOne('/api/review/sessions/current')
      .flush(null, { status: 204, statusText: 'No Content' });
    http
      .expectOne(
        '/api/review/overview?scope=concept&domain=dotnet&topic=clr-memory&concept=gc-modes',
      )
      .flush(overview({ newCount: 3, totalEligible: 3 }));
    http.expectOne('/api/review/coverage').flush(coverage);
    await settle(fixture);

    expect(select('scopeType').value).toBe('concept');
    expect(select('topic').value).toBe('clr-memory');
    expect(select('concept').value).toBe('gc-modes');
    expect(Array.from(select('concept').options).map((o) => o.value)).toEqual([
      '',
      'stack-vs-heap',
      'gc-modes',
    ]);
    expect(text()).toContain('3 new questions');

    const start = vi.spyOn(store, 'start').mockResolvedValue(true);
    startButton().click();
    expect(start).toHaveBeenCalledWith({
      scope: 'concept',
      domain: 'dotnet',
      topic: 'clr-memory',
      concept: 'gc-modes',
      totalLimit: 20,
      newLimit: 10,
    });

    // Clearing the topic clears and disables the concept; no request for an incomplete scope.
    setSelect('topic', '');
    await settle(fixture);
    expect(select('concept').value).toBe('');
    expect(select('concept').disabled).toBe(true);
    expect(text()).toContain('Pick a domain, a topic and a concept');
    expect(navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({ queryParams: { scope: 'concept', domain: 'dotnet' } }),
    );

    setSelect('topic', 'clr-memory');
    await settle(fixture);
    expect(select('concept').disabled).toBe(false);
    setSelect('concept', 'stack-vs-heap');
    http
      .expectOne(
        '/api/review/overview?scope=concept&domain=dotnet&topic=clr-memory&concept=stack-vs-heap',
      )
      .flush(overview());
    await settle(fixture);
    expect(navigate).toHaveBeenLastCalledWith(
      [],
      expect.objectContaining({
        queryParams: {
          scope: 'concept',
          domain: 'dotnet',
          topic: 'clr-memory',
          concept: 'stack-vs-heap',
        },
      }),
    );
  });

  describe('weak mode', () => {
    const modeButton = (label: string) =>
      Array.from(root().querySelectorAll<HTMLButtonElement>('button[aria-pressed]')).find((b) =>
        b.textContent?.includes(label),
      )!;

    async function openWeak(overviewBody = overview({ weakCount: 4 })) {
      queryParams.next(convertToParamMap({ mode: 'weak' }));
      await open(overviewBody);
    }

    it('?mode=weak preselects the weak mode, shows the weak count and hides the new limit', async () => {
      await openWeak(overview({ weakCount: 4, dueCount: 7, newCount: 28 }));

      expect(modeButton('Weak spots').getAttribute('aria-pressed')).toBe('true');
      expect(modeButton('Due & new').getAttribute('aria-pressed')).toBe('false');
      expect(text()).toContain('4 questions to drill');
      expect(text()).not.toContain('28 new questions');
      expect(text()).not.toContain('due for review');
      expect(numberInput('newLimit')).toBeNull();
      expect(numberInput('totalLimit')).not.toBeNull();
      expect(startButton().disabled).toBe(false);
    });

    it('starts a weak session with mode "weak" and no new limit', async () => {
      await openWeak();
      const start = vi.spyOn(store, 'start').mockResolvedValue(true);

      await setNumber('totalLimit', '12');
      startButton().click();

      expect(start).toHaveBeenCalledWith({ scope: 'all', mode: 'weak', totalLimit: 12 });
    });

    it('disables Start with a message when nothing in scope is weak', async () => {
      await openWeak(overview({ weakCount: 0, dueCount: 3 }));

      expect(text()).toContain('Nothing to drill');
      expect(startButton().disabled).toBe(true);
    });

    it('the mode toggle writes ?mode=weak to the URL and back, keeping the scope', async () => {
      await open(overview({ weakCount: 2 }));
      expect(modeButton('Due & new').getAttribute('aria-pressed')).toBe('true');
      expect(numberInput('newLimit')).not.toBeNull();

      modeButton('Weak spots').click();
      await settle(fixture);
      expect(navigate).toHaveBeenLastCalledWith(
        [],
        expect.objectContaining({ queryParams: { mode: 'weak' }, replaceUrl: true }),
      );
      expect(text()).toContain('2 questions to drill');
      expect(numberInput('newLimit')).toBeNull();

      setSelect('scopeType', 'domain');
      setSelect('domain', 'dotnet');
      http.expectOne('/api/review/overview?scope=domain&domain=dotnet').flush(overview());
      await settle(fixture);
      expect(navigate).toHaveBeenLastCalledWith(
        [],
        expect.objectContaining({
          queryParams: { scope: 'domain', domain: 'dotnet', mode: 'weak' },
        }),
      );

      modeButton('Due & new').click();
      await settle(fixture);
      expect(navigate).toHaveBeenLastCalledWith(
        [],
        expect.objectContaining({ queryParams: { scope: 'domain', domain: 'dotnet' } }),
      );
      expect(numberInput('newLimit')).not.toBeNull();
    });

    it('explains a refused weak start (409 "Nothing to drill") and refreshes the counts', async () => {
      await openWeak(overview({ weakCount: 1 }));

      startButton().click();
      http
        .expectOne('/api/review/sessions')
        .flush({ title: 'Nothing to drill', status: 409 }, { status: 409, statusText: 'Conflict' });
      // Let the refused start settle; the counts are then re-read (whenStable would wait on them).
      await new Promise((resolve) => setTimeout(resolve));
      TestBed.tick();
      http.expectOne('/api/review/overview?scope=all').flush(overview({ weakCount: 0 }));
      await settle(fixture);

      // Said once, inline with the refreshed count — not also as an alert.
      expect(root().querySelector('[role=alert]')).toBeNull();
      expect(text().split('Nothing to drill').length - 1).toBe(1);
      expect(startButton().disabled).toBe(true);
    });

    it('keeps the refusal as an alert while the refreshed count still shows weak questions', async () => {
      await openWeak(overview({ weakCount: 1 }));

      startButton().click();
      http
        .expectOne('/api/review/sessions')
        .flush({ title: 'Nothing to drill', status: 409 }, { status: 409, statusText: 'Conflict' });
      await new Promise((resolve) => setTimeout(resolve));
      TestBed.tick();
      http.expectOne('/api/review/overview?scope=all').flush(overview({ weakCount: 1 }));
      await settle(fixture);

      expect(root().querySelector('[role=alert]')?.textContent).toContain('Nothing to drill');
    });

    it('lands on ?mode=weak&scope=concept&… with the weak mode and the concept preselected', async () => {
      queryParams.next(
        convertToParamMap({
          mode: 'weak',
          scope: 'concept',
          domain: 'dotnet',
          topic: 'clr-memory',
          concept: 'gc-modes',
        }),
      );
      fixture = TestBed.createComponent(ReviewOverview);
      fixture.detectChanges();
      TestBed.tick();
      http.expectOne('/api/knowledge/tree').flush(conceptTree);
      http
        .expectOne('/api/review/sessions/current')
        .flush(null, { status: 204, statusText: 'No Content' });
      http
        .expectOne(
          '/api/review/overview?scope=concept&domain=dotnet&topic=clr-memory&concept=gc-modes',
        )
        .flush(overview({ weakCount: 2 }));
      http.expectOne('/api/review/coverage').flush(coverage);
      await settle(fixture);

      expect(modeButton('Weak spots').getAttribute('aria-pressed')).toBe('true');
      expect(select('concept').value).toBe('gc-modes');
      expect(text()).toContain('2 questions to drill');
      for (const [, extras] of navigate.mock.calls) {
        // Never an intermediate URL that drops the weak mode or the concept.
        expect((extras as { queryParams: Record<string, string> }).queryParams).toEqual({
          scope: 'concept',
          domain: 'dotnet',
          topic: 'clr-memory',
          concept: 'gc-modes',
          mode: 'weak',
        });
      }
    });
  });
});
