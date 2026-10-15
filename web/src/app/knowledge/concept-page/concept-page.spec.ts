import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { ReadinessStore } from '../readiness/readiness-store';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeConcept } from '../api/knowledge-concept.models';
import {
  ATLAS_TREE,
  ATLAS_TREE_UK,
  conceptPage,
  flushAtlas,
  flushTree,
  settle,
} from '../testing/atlas-fixtures';
import { ConceptPage } from './concept-page';

const URL = '/api/knowledge/concepts/dotnet/clr-memory';

describe('ConceptPage', () => {
  let fixture: ComponentFixture<ConceptPage>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');
  /** jsdom's selector engine trips over `?`/`&` inside attribute values, so match hrefs by hand. */
  const linkTo = (href: string) =>
    Array.from(root().querySelectorAll('a')).find((a) => a.getAttribute('href') === href)!;
  const neighbours = () =>
    root().querySelector<HTMLElement>('nav[aria-label="Neighbouring concepts"]');

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [ConceptPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        ReadinessStore,
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    TestBed.inject(I18n).set('en');
    http.verify();
  });

  async function open(
    concept: string,
    body: KnowledgeConcept | null = conceptPage({ slug: concept }),
    tree = ATLAS_TREE,
  ) {
    fixture = TestBed.createComponent(ConceptPage);
    fixture.componentRef.setInput('domain', 'dotnet');
    fixture.componentRef.setInput('topic', 'clr-memory');
    fixture.componentRef.setInput('concept', concept);
    flushAtlas(http, tree);
    const lang = TestBed.inject(I18n).lang();
    const req = http.expectOne(`${URL}/${concept}${lang === 'uk' ? '?lang=uk' : ''}`);
    if (body) {
      req.flush(body);
    } else {
      req.flush(null, { status: 404, statusText: 'Not Found' });
    }
    await settle(fixture);
  }

  it('renders the title, the note with highlighted code, level, question and kata counts', async () => {
    await open('gc-modes');

    expect(root().querySelector('h1')?.textContent).toContain('GC modes');
    const note = root().querySelector('app-concept-note')!;
    expect(note.querySelector('h2')?.textContent).toBe('Workstation vs server');
    expect(note.querySelector('strong')?.textContent).toBe('one heap per core');
    expect(note.querySelector('pre [class^=hljs-]')).not.toBeNull();
    expect(text()).toContain('Senior');
    expect(text()).toContain('2 questions');
    expect(text()).toContain('1 kata');
  });

  it('links to the previous and next concept of the topic, skipping deprecated ones', async () => {
    await open('gc-modes');

    const prev = neighbours()!.querySelector<HTMLAnchorElement>('a[rel=prev]')!;
    const next = neighbours()!.querySelector<HTMLAnchorElement>('a[rel=next]')!;
    expect(prev.getAttribute('href')).toBe('/atlas/dotnet/clr-memory/stack-vs-heap');
    expect(prev.textContent).toContain('Stack vs heap');
    expect(next.getAttribute('href')).toBe('/atlas/dotnet/clr-memory/span-memory');
    expect(next.textContent).toContain('Span<T>, Memory<T>');
  });

  it('has no "next" on the last concept and "previous" leads to the preceding one', async () => {
    await open('span-memory', conceptPage({ slug: 'span-memory', title: 'Span<T>, Memory<T>' }));

    expect(neighbours()!.querySelector('a[rel=next]')).toBeNull();
    expect(neighbours()!.querySelector('a[rel=prev]')?.getAttribute('href')).toBe(
      '/atlas/dotnet/clr-memory/gc-modes',
    );
  });

  it('shows "not found" with a link back to the Atlas on a 404', async () => {
    await open('nope', null);

    expect(root().querySelector('app-atlas-not-found')).not.toBeNull();
    expect(root().querySelector('a[href="/atlas"]')).not.toBeNull();
  });

  it('treats a deprecated concept as not found', async () => {
    await open('retired', conceptPage({ slug: 'retired', deprecatedAt: '2026-01-01T00:00:00Z' }));

    expect(root().querySelector('app-atlas-not-found')).not.toBeNull();
  });

  it('shows "no note yet" when the note is empty', async () => {
    await open('gc-modes', conceptPage({ note: '' }));

    expect(text()).toContain('No note for this concept yet.');
    expect(root().querySelector('app-concept-note')).toBeNull();
  });

  it('carries the unreviewed badge on an unreviewed concept', async () => {
    await open('gc-modes', conceptPage({ reviewed: false }));

    expect(root().querySelector('header app-unreviewed-badge')?.textContent).toContain(
      'unreviewed',
    );
  });

  it('marks an English fallback with the EN badge and lang="en" under Ukrainian', async () => {
    TestBed.inject(I18n).set('uk');
    await open('gc-modes', conceptPage({ language: 'en', fallback: true }), ATLAS_TREE_UK);

    expect(root().querySelector('header app-en-badge')?.textContent).toContain('EN');
    expect(root().querySelector('h1 [lang=en]')?.textContent).toContain('GC modes');
    expect(root().querySelector('app-concept-note')?.getAttribute('lang')).toBe('en');
  });

  it('shows a Ukrainian note without the EN badge', async () => {
    TestBed.inject(I18n).set('uk');
    await open(
      'gc-modes',
      conceptPage({ language: 'uk', title: 'Режими GC', note: 'Серверний GC' }),
      ATLAS_TREE_UK,
    );

    expect(root().querySelector('h1')?.textContent).toContain('Режими GC');
    expect(root().querySelector('header app-en-badge')?.textContent?.trim() ?? '').toBe('');
    expect(root().querySelector('app-concept-note')?.getAttribute('lang')).toBe('uk');
  });

  it('re-requests the concept with ?lang=uk on a language switch, keeping the page meanwhile', async () => {
    await open('gc-modes');

    TestBed.inject(I18n).set('uk');
    TestBed.tick();
    flushTree(http, ATLAS_TREE_UK);
    const req = http.expectOne(`${URL}/gc-modes?lang=uk`);
    TestBed.tick();
    expect(root().querySelector('h1')?.textContent).toContain('GC modes');

    req.flush(conceptPage({ language: 'uk', title: 'Режими GC' }));
    await settle(fixture);
    expect(root().querySelector('h1')?.textContent).toContain('Режими GC');
  });

  it('shows the concept readiness state', async () => {
    await open('gc-modes');

    expect(root().querySelector('header app-readiness-chip')?.textContent).toContain('in progress');
  });

  it('links to a review of just this concept', async () => {
    await open('gc-modes');

    const link = linkTo('/review?scope=concept&domain=dotnet&topic=clr-memory&concept=gc-modes');
    expect(link.textContent).toContain('Review this concept');
  });

  it('does not show the previous concept while the next one loads', async () => {
    await open('gc-modes');
    expect(root().querySelector('h1')?.textContent).toContain('GC modes');

    fixture.componentRef.setInput('concept', 'span-memory');
    TestBed.tick();
    const req = http.expectOne(`${URL}/span-memory`);
    expect(root().querySelector('h1')).toBeNull();
    expect(text()).toContain('Loading the note');
    expect(root().querySelector('app-readiness-chip')).toBeNull();

    req.flush(conceptPage({ slug: 'span-memory', title: 'Span<T>, Memory<T>' }));
    await settle(fixture);
    expect(root().querySelector('h1')?.textContent).toContain('Span<T>, Memory<T>');
    expect(neighbours()!.querySelector('a[rel=next]')).toBeNull();
  });

  it('shows an error with Retry on a server error and recovers', async () => {
    fixture = TestBed.createComponent(ConceptPage);
    fixture.componentRef.setInput('domain', 'dotnet');
    fixture.componentRef.setInput('topic', 'clr-memory');
    fixture.componentRef.setInput('concept', 'gc-modes');
    flushAtlas(http, ATLAS_TREE);
    http.expectOne(`${URL}/gc-modes`).flush(null, { status: 500, statusText: 'Server Error' });
    await settle(fixture);

    expect(root().querySelector('[role=alert]')?.textContent).toContain(
      'Could not load the concept',
    );
    expect(root().querySelector('app-atlas-not-found')).toBeNull();
    root().querySelector<HTMLButtonElement>('[role=alert] button')!.click();
    TestBed.tick();
    http.expectOne(`${URL}/gc-modes`).flush(conceptPage());
    await settle(fixture);
    expect(root().querySelector('h1')?.textContent).toContain('GC modes');
  });

  it('hides "Review this concept" when the concept has no questions', async () => {
    await open('gc-modes', conceptPage({ questionCount: 0 }));

    expect(text()).not.toContain('Review this concept');
  });

  it('puts the concept title into the document title', async () => {
    await open('gc-modes');

    expect(TestBed.inject(Title).getTitle()).toBe('GC modes · Spine');
    fixture.destroy();
    TestBed.tick();
    expect(TestBed.inject(Title).getTitle()).toBe('Spine');
  });
});
