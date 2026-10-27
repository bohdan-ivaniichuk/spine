import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  ATLAS_TREE,
  ATLAS_TREE_UK,
  flushTree,
  settle,
} from '../../knowledge/testing/atlas-fixtures';
import { I18n } from '../../shared/i18n/i18n';
import { WEAK_SPOTS_URL, WeakSpots as WeakSpotsDto } from '../api/weak-spots.models';
import { WeakSpots } from './weak-spots';

/** Hand-made payload shaped like contracts/weak-spots-api.md; served order is the ranking. */
function weakSpots(overrides: Partial<WeakSpotsDto> = {}): WeakSpotsDto {
  return {
    since: '2026-09-27T10:00:00+00:00',
    windowDays: 30,
    minAnswers: 3,
    concepts: [
      {
        domain: 'dotnet',
        topic: 'clr-memory',
        concept: 'gc-modes',
        answers: 6,
        wrong: 4,
        accuracy: 0.3333,
        lastAnsweredAt: '2026-10-26T18:02:11+00:00',
        weakQuestions: 2,
        readiness: { concept: 'gc-modes', state: 'in-progress', questions: 2, readyQuestions: 1 },
      },
      {
        domain: 'angular',
        topic: 'signals',
        concept: 'signals-basics',
        answers: 4,
        wrong: 2,
        accuracy: 0.5,
        lastAnsweredAt: '2026-10-20T09:00:00+00:00',
        weakQuestions: 0,
        readiness: null,
      },
    ],
    topics: [
      {
        domain: 'dotnet',
        topic: 'clr-memory',
        answers: 14,
        wrong: 6,
        accuracy: 0.5714,
        lastAnsweredAt: '2026-10-26T18:02:11+00:00',
        weakQuestions: 3,
      },
    ],
    ...overrides,
  };
}

describe('WeakSpots', () => {
  let fixture: ComponentFixture<WeakSpots>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');
  /** jsdom's selector engine trips over `?`/`&` inside attribute values, so match hrefs by hand. */
  const hrefs = (el: Element) =>
    Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
  const conceptRows = () =>
    Array.from(root().querySelectorAll<HTMLElement>('[data-testid=weak-concept]'));
  const topicRows = () =>
    Array.from(root().querySelectorAll<HTMLElement>('[data-testid=weak-topic]'));

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [WeakSpots],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    TestBed.inject(I18n).set('en');
    http.verify();
  });

  async function open(body: WeakSpotsDto = weakSpots(), tree = ATLAS_TREE) {
    fixture = TestBed.createComponent(WeakSpots);
    TestBed.tick();
    http.expectOne(WEAK_SPOTS_URL).flush(body);
    flushTree(http, tree);
    await settle(fixture);
  }

  it('lists the weakest concepts in the served order with their figures and readiness', async () => {
    await open();

    const rows = conceptRows();
    expect(rows).toHaveLength(2);
    expect(rows[0].querySelector('h3')?.textContent).toContain('GC modes');
    expect(rows[1].querySelector('h3')?.textContent).toContain('Signal basics');

    const first = rows[0].textContent!.replace(/\s+/g, ' ');
    expect(first).toContain('C# & .NET Runtime › Memory & GC');
    expect(first).toContain('33%');
    expect(first).toContain('4 of 6 wrong');
    expect(first).toMatch(/26 Oct/);
    expect(first).toContain('in progress');
    expect(first).toContain('2 questions to drill');
    // No readiness for a concept without schedulable questions.
    expect(rows[1].querySelector('app-readiness-chip')?.textContent?.trim() ?? '').toBe('');
  });

  it('links each concept to its note and drills it in weak mode', async () => {
    await open();

    const [first, second] = conceptRows();
    expect(hrefs(first)).toEqual([
      '/atlas/dotnet/clr-memory/gc-modes',
      '/review?mode=weak&scope=concept&domain=dotnet&topic=clr-memory&concept=gc-modes',
    ]);
    // The drill is named "Drill" + the concept title (which keeps its own lang attribute).
    const drill = first.querySelectorAll('a')[1];
    const names = drill
      .getAttribute('aria-labelledby')!
      .split(' ')
      .map((id) => root().querySelector(`[id="${id}"]`)?.textContent?.trim());
    expect(names).toEqual(['Drill', 'GC modes']);
    // Nothing currently wrong in that concept: no drill to offer, only the note.
    expect(hrefs(second)).toEqual(['/atlas/angular/signals/signals-basics']);
  });

  it('ranks topics with a link to the topic and a topic drill', async () => {
    await open();

    const rows = topicRows();
    expect(rows).toHaveLength(1);
    expect(rows[0].textContent).toContain('Memory & GC');
    expect(rows[0].textContent).toContain('57%');
    expect(rows[0].textContent).toContain('6 of 14 wrong');
    expect(hrefs(rows[0])).toEqual([
      '/atlas/dotnet/clr-memory',
      '/review?mode=weak&scope=topic&domain=dotnet&topic=clr-memory',
    ]);
  });

  it('explains the window from the payload and offers a drill across everything', async () => {
    await open(weakSpots({ windowDays: 14, minAnswers: 5 }));

    expect(root().querySelector('h1')?.textContent).toContain('Weak spots');
    const lead = root().querySelector('header')!.textContent!.replace(/\s+/g, ' ');
    expect(lead).toContain('14 days');
    expect(lead).toContain('at least 5 answers');
    expect(hrefs(root().querySelector('header')!)).toContain('/review?mode=weak');
  });

  it('shows titles in Ukrainian with the EN badge on fallbacks, without ?lang on the stats', async () => {
    TestBed.inject(I18n).set('uk');
    fixture = TestBed.createComponent(WeakSpots);
    TestBed.tick();
    http.expectOne(WEAK_SPOTS_URL).flush(weakSpots());
    http.expectOne('/api/knowledge/tree?lang=uk').flush(ATLAS_TREE_UK);
    await settle(fixture);

    const first = conceptRows()[0];
    const line = first.textContent!.replace(/\s+/g, ' ');
    expect(line).toContain("C# та середовище .NET › Пам'ять і GC");
    expect(first.querySelector('h3 [lang=en]')?.textContent).toContain('GC modes');
    expect(first.querySelector('app-en-badge')?.textContent).toContain('EN');
    expect(line).toContain('4 з 6 неправильно');
    expect(conceptRows()[1].querySelector('h3 [lang=en]')?.textContent).toContain('Signal basics');
  });

  it('shows an empty state linking to Review when nothing is ranked', async () => {
    await open(weakSpots({ concepts: [], topics: [] }));

    expect(conceptRows()).toHaveLength(0);
    expect(text()).toContain('No weak spots yet');
    expect(text()).toContain('at least 3 answers');
    expect(hrefs(root())).toContain('/review');
    expect(root().querySelector('[data-testid=weak-topics]')).toBeNull();
  });

  it('still shows the topic ranking when no single concept qualifies', async () => {
    await open(weakSpots({ concepts: [] }));

    expect(text()).toContain('No weak spots yet');
    expect(topicRows()).toHaveLength(1);
  });

  it('shows an error with a retry that re-requests', async () => {
    fixture = TestBed.createComponent(WeakSpots);
    TestBed.tick();
    http.expectOne(WEAK_SPOTS_URL).flush(null, { status: 500, statusText: 'Server Error' });
    flushTree(http);
    await settle(fixture);

    expect(root().querySelector('[role=alert]')?.textContent).toContain(
      'Could not load your weak spots',
    );
    root().querySelector<HTMLButtonElement>('[role=alert] button')!.click();
    TestBed.tick();
    http.expectOne(WEAK_SPOTS_URL).flush(weakSpots());
    await settle(fixture);
    expect(conceptRows()).toHaveLength(2);
  });
});
