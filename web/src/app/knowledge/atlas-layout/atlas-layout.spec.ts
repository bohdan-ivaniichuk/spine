import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ViewportScroller } from '@angular/common';
import { Router } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { KNOWLEDGE_ROUTES } from '../knowledge.routes';
import {
  ATLAS_TREE,
  ATLAS_TREE_UK,
  conceptPage,
  flushAtlas,
  flushReadiness,
  flushTree,
  READINESS,
  settle,
} from '../testing/atlas-fixtures';

describe('AtlasLayout', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  const root = () => harness.routeNativeElement as HTMLElement;
  const crumbs = () =>
    Array.from(root().querySelectorAll<HTMLElement>('nav[aria-label=Breadcrumb] li')).map((li) => ({
      text: li.textContent?.replace(/\s+/g, ' ').trim(),
      href: li.querySelector('a')?.getAttribute('href') ?? null,
      current: li.querySelector('[aria-current=page]') !== null,
    }));

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(
          [
            { path: 'atlas', children: KNOWLEDGE_ROUTES },
            { path: 'review', children: [] },
          ],
          withComponentInputBinding(),
        ),
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => {
    TestBed.inject(I18n).set('en');
    http.verify();
  });

  async function go(url: string, tree = ATLAS_TREE, concept?: () => void) {
    await harness.navigateByUrl(url);
    flushAtlas(http, tree);
    concept?.();
    await settle(harness.fixture);
    harness.detectChanges();
  }

  it('shows only "Atlas" on the atlas home', async () => {
    await go('/atlas');

    expect(crumbs()).toEqual([{ text: 'Atlas', href: null, current: true }]);
  });

  it('shows Atlas › domain › topic with links to the upper levels', async () => {
    await go('/atlas/dotnet/clr-memory');

    expect(crumbs()).toEqual([
      { text: 'Atlas', href: '/atlas', current: false },
      { text: 'C# & .NET Runtime', href: '/atlas/dotnet', current: false },
      { text: 'Memory & GC', href: null, current: true },
    ]);
  });

  it('shows the concept as the last crumb on a concept URL (reload-safe)', async () => {
    await go('/atlas/dotnet/clr-memory/gc-modes', ATLAS_TREE, () =>
      http.expectOne('/api/knowledge/concepts/dotnet/clr-memory/gc-modes').flush(conceptPage()),
    );

    expect(crumbs().map((c) => c.text)).toEqual([
      'Atlas',
      'C# & .NET Runtime',
      'Memory & GC',
      expect.stringContaining('GC modes'),
    ]);
    expect(crumbs()[2].href).toBe('/atlas/dotnet/clr-memory');
    expect(crumbs()[3].current).toBe(true);
  });

  it('re-fetches the knowledge tree on entering the Atlas', async () => {
    const source = TestBed.inject(KnowledgeTreeSource);
    source.tree.value(); // create the shared resource as the rail would
    flushTree(http, ATLAS_TREE);
    await settle(harness.fixture);

    await harness.navigateByUrl('/atlas');
    TestBed.tick();
    const requests = http.match('/api/knowledge/tree');
    expect(requests).toHaveLength(1);
    requests[0].flush(ATLAS_TREE);
    flushReadiness(http);
    await settle(harness.fixture);
  });

  it('marks fallback crumbs with the EN badge under Ukrainian', async () => {
    TestBed.inject(I18n).set('uk');
    await go('/atlas/angular/signals', ATLAS_TREE_UK);

    const nav = root().querySelector('nav[aria-label="Навігаційний ланцюжок"]')!;
    const badges = Array.from(nav.querySelectorAll('app-en-badge')).filter((b) =>
      b.textContent?.includes('EN'),
    );
    expect(badges).toHaveLength(2);
    expect(
      nav.querySelector('a[href="/atlas/angular"] [lang=en], a[href="/atlas/angular"][lang=en]'),
    ).not.toBeNull();
  });

  it('requests the tree and readiness exactly once on entry', async () => {
    await harness.navigateByUrl('/atlas');
    TestBed.tick();
    http.expectOne('/api/knowledge/tree').flush(ATLAS_TREE);
    http.expectOne('/api/review/readiness').flush(READINESS);
    await settle(harness.fixture);
  });

  it('fetches readiness again after leaving and re-entering the Atlas', async () => {
    await go('/atlas');

    await harness.navigateByUrl('/review');
    TestBed.tick();
    http.expectNone('/api/review/readiness');

    await harness.navigateByUrl('/atlas');
    TestBed.tick();
    http.expectOne('/api/review/readiness').flush(READINESS);
    flushTree(http);
    await settle(harness.fixture);
  });

  it('scrolls to the top and moves focus to the page after a prev/next navigation', async () => {
    const scroll = vi.spyOn(TestBed.inject(ViewportScroller), 'scrollToPosition').mockReturnValue();
    await go('/atlas/dotnet/clr-memory/gc-modes', ATLAS_TREE, () =>
      http.expectOne('/api/knowledge/concepts/dotnet/clr-memory/gc-modes').flush(conceptPage()),
    );
    expect(scroll).not.toHaveBeenCalled();

    const next = root().querySelector<HTMLAnchorElement>('a[rel=next]')!;
    next.focus();
    expect(document.activeElement).toBe(next);
    await TestBed.inject(Router).navigateByUrl('/atlas/dotnet/clr-memory/span-memory');
    TestBed.tick();
    http
      .expectOne('/api/knowledge/concepts/dotnet/clr-memory/span-memory')
      .flush(conceptPage({ slug: 'span-memory' }));
    await settle(harness.fixture);

    expect(scroll).toHaveBeenCalledWith([0, 0]);
    const top = root().querySelector('app-atlas-layout > div')!;
    expect(document.activeElement).toBe(top);
    expect(top.getAttribute('tabindex')).toBe('-1');
  });
});
