import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ReadinessStore } from '../readiness/readiness-store';
import { I18n } from '../../shared/i18n/i18n';
import { ATLAS_TREE, ATLAS_TREE_UK, flushAtlas, settle } from '../testing/atlas-fixtures';
import { AtlasHome } from './atlas-home';

describe('AtlasHome', () => {
  let fixture: ComponentFixture<AtlasHome>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [AtlasHome],
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

  async function open(tree = ATLAS_TREE) {
    fixture = TestBed.createComponent(AtlasHome);
    flushAtlas(http, tree);
    await settle(fixture);
  }

  it('lists live domains in manifest order with number, colour, title and concept count', async () => {
    await open();

    const domains = Array.from(root().querySelectorAll('[data-testid=atlas-domain]'));
    expect(domains).toHaveLength(2);
    expect(domains[0].textContent).toContain('01');
    expect(domains[0].textContent).toContain('C# & .NET Runtime');
    expect(domains[0].textContent).toContain('3 concepts');
    expect(domains[0].querySelector('.domain-1')).not.toBeNull();
    expect(domains[0].querySelector('a[href="/atlas/dotnet"]')).not.toBeNull();
    expect(domains[1].textContent).toContain('02');
    expect(domains[1].textContent).toContain('Angular');
    expect(domains[1].textContent).toContain('1 concept');
    expect(domains[1].querySelector('.domain-2')).not.toBeNull();
    expect(text()).not.toContain('Old domain');
  });

  it('lists topics in order, hides deprecated ones and links each to its URL', async () => {
    await open();

    const topics = Array.from(
      root().querySelectorAll('[data-testid=atlas-domain]')[0].querySelectorAll('li'),
    ).map((li) => li.textContent?.replace(/\s+/g, ' ').trim());
    expect(topics[0]).toContain('Memory & GC');
    expect(topics[0]).toContain('3');
    expect(topics[1]).toContain('Async & threading');
    expect(topics).toHaveLength(2);
    expect(text()).not.toContain('Old topic');
    expect(root().querySelector('a[href="/atlas/dotnet/clr-memory"]')).not.toBeNull();
  });

  it('marks a topic without concepts as "soon" and does not link it', async () => {
    await open();

    expect(root().querySelector('a[href="/atlas/dotnet/async"]')).toBeNull();
    const soon = root().querySelector('[aria-disabled=true]')!;
    expect(soon.textContent).toContain('Async & threading');
    expect(soon.textContent).toContain('soon');
  });

  it('shows a fallback domain title with the EN badge and lang="en" under Ukrainian', async () => {
    TestBed.inject(I18n).set('uk');
    await open(ATLAS_TREE_UK);

    const domains = Array.from(root().querySelectorAll('[data-testid=atlas-domain]'));
    expect(domains[0].textContent).toContain('C# та середовище .NET');
    expect(domains[0].querySelector('h2 app-en-badge')?.textContent?.trim() ?? '').toBe('');
    const angular = domains[1].querySelector('[lang=en]');
    expect(angular?.textContent).toContain('Angular');
    expect(domains[1].querySelector('h2 app-en-badge')?.textContent).toContain('EN');
  });

  it('shows readiness per domain and topic, and no percentage where nothing counts', async () => {
    await open();

    const [dotnet, angular] = Array.from(root().querySelectorAll('[data-testid=atlas-domain]'));
    expect(dotnet.querySelector('header + app-readiness-bar')?.textContent).toContain('25%');
    const memory = dotnet.querySelector('a[href="/atlas/dotnet/clr-memory"]')!.closest('li')!;
    expect(memory.querySelector('app-readiness-bar')?.textContent).toContain('25%');
    expect(angular.textContent).not.toContain('%');
    expect(angular.querySelector('app-readiness-bar')?.textContent).toContain('No readiness yet');
  });
});
