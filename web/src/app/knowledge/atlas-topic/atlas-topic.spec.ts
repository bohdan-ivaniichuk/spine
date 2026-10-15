import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { ReadinessStore } from '../readiness/readiness-store';
import { I18n } from '../../shared/i18n/i18n';
import {
  ATLAS_TREE,
  ATLAS_TREE_UK,
  flushAtlas,
  flushTree,
  settle,
} from '../testing/atlas-fixtures';
import { AtlasTopic } from './atlas-topic';

describe('AtlasTopic', () => {
  let fixture: ComponentFixture<AtlasTopic>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');
  /** jsdom's selector engine trips over `?`/`&` inside attribute values, so match hrefs by hand. */
  const linkTo = (href: string) =>
    Array.from(root().querySelectorAll('a')).find((a) => a.getAttribute('href') === href)!;
  const items = () =>
    Array.from(root().querySelectorAll<HTMLElement>('[data-testid=atlas-concept]'));

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [AtlasTopic],
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

  async function open(domain: string, topic: string, tree = ATLAS_TREE) {
    fixture = TestBed.createComponent(AtlasTopic);
    fixture.componentRef.setInput('domain', domain);
    fixture.componentRef.setInput('topic', topic);
    flushAtlas(http, tree);
    await settle(fixture);
  }

  it('lists live concepts in order with title, level and question count, linking to the concept', async () => {
    await open('dotnet', 'clr-memory');

    expect(root().querySelector('h1')?.textContent).toContain('Memory & GC');
    const concepts = items();
    expect(concepts.map((c) => c.querySelector('a')?.getAttribute('href'))).toEqual([
      '/atlas/dotnet/clr-memory/stack-vs-heap',
      '/atlas/dotnet/clr-memory/gc-modes',
      '/atlas/dotnet/clr-memory/span-memory',
    ]);
    expect(concepts[0].textContent).toContain('Stack vs heap');
    expect(concepts[0].textContent).toContain('M');
    expect(concepts[0].textContent).toContain('3 questions');
    expect(concepts[1].textContent).toContain('S');
    expect(text()).not.toContain('Retired concept');
  });

  it('shows the "soon" state for a topic without concepts', async () => {
    await open('dotnet', 'async');

    expect(root().querySelector('h1')?.textContent).toContain('Async & threading');
    expect(items()).toHaveLength(0);
    expect(text()).toContain('soon');
  });

  it('shows "not found" for an unknown or deprecated topic', async () => {
    await open('dotnet', 'old-topic');

    expect(root().querySelector('app-atlas-not-found')).not.toBeNull();
    expect(root().querySelector('a[href="/atlas"]')).not.toBeNull();
  });

  it('shows "not found" when the topic exists under another domain only', async () => {
    await open('angular', 'clr-memory');

    expect(root().querySelector('app-atlas-not-found')).not.toBeNull();
  });

  it('carries the unreviewed badge on unreviewed concepts in either language', async () => {
    await open('dotnet', 'clr-memory');
    expect(items()[1].querySelector('app-unreviewed-badge')?.textContent).toContain('unreviewed');
    expect(items()[0].querySelector('app-unreviewed-badge')?.textContent?.trim() ?? '').toBe('');

    TestBed.inject(I18n).set('uk');
    flushTree(http, ATLAS_TREE_UK);
    await settle(fixture);
    expect(items()[1].querySelector('app-unreviewed-badge')?.textContent).toContain(
      'не перевірено',
    );
  });

  it('marks fallback concept titles with the EN badge and lang="en" under Ukrainian', async () => {
    TestBed.inject(I18n).set('uk');
    await open('dotnet', 'clr-memory', ATLAS_TREE_UK);

    const gc = items()[1];
    expect(gc.querySelector('[lang=en]')?.textContent).toContain('GC modes');
    expect(gc.querySelector('app-en-badge')?.textContent).toContain('EN');
    expect(items()[0].querySelector('[lang=en]')).toBeNull();
    expect(items()[0].querySelector('app-en-badge')?.textContent?.trim() ?? '').toBe('');
  });

  it('shows a 25% readiness bar on the topic and a state chip per concept', async () => {
    await open('dotnet', 'clr-memory');

    const bar = root().querySelector('header app-readiness-bar')!;
    expect(bar.textContent).toContain('25%');
    expect(bar.querySelector<HTMLElement>('[data-testid=readiness-fill]')?.style.width).toBe('25%');
    expect(
      items().map((c) => c.querySelector('app-readiness-chip')?.textContent?.split(':')[0].trim()),
    ).toEqual(['ready', 'in progress', 'not started']);
  });

  it('shows no percentage for a topic where nothing counts', async () => {
    await open('dotnet', 'async');

    const bar = root().querySelector('header app-readiness-bar')!;
    expect(bar.textContent).not.toContain('%');
    expect(bar.querySelector('[data-testid=readiness-fill]')).toBeNull();
    expect(bar.querySelector('.sr-only')?.textContent).toContain('No readiness yet');
  });

  it('links to a review of the topic', async () => {
    await open('dotnet', 'clr-memory');

    const link = linkTo('/review?scope=topic&domain=dotnet&topic=clr-memory');
    expect(link.textContent).toContain('Review this topic');
  });

  it('puts the topic title into the document title, in the active language', async () => {
    await open('dotnet', 'clr-memory');
    expect(TestBed.inject(Title).getTitle()).toBe('Memory & GC · Spine');

    TestBed.inject(I18n).set('uk');
    flushTree(http, ATLAS_TREE_UK);
    await settle(fixture);
    expect(TestBed.inject(Title).getTitle()).toBe("Пам'ять і GC · Spine");
  });
});
