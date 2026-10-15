import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ReadinessStore } from '../readiness/readiness-store';
import { ATLAS_TREE, flushAtlas, settle } from '../testing/atlas-fixtures';
import { AtlasDomain } from './atlas-domain';

describe('AtlasDomain', () => {
  let fixture: ComponentFixture<AtlasDomain>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');
  /** jsdom's selector engine trips over `?`/`&` inside attribute values, so match hrefs by hand. */
  const linkTo = (href: string) =>
    Array.from(root().querySelectorAll('a')).find((a) => a.getAttribute('href') === href)!;

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [AtlasDomain],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        ReadinessStore,
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function open(domain: string) {
    fixture = TestBed.createComponent(AtlasDomain);
    fixture.componentRef.setInput('domain', domain);
    flushAtlas(http, ATLAS_TREE);
    await settle(fixture);
  }

  it('shows the domain with its number and colour and lists its live topics', async () => {
    await open('dotnet');

    expect(root().querySelector('h1')?.textContent).toContain('C# & .NET Runtime');
    expect(root().querySelector('.domain-1')?.textContent).toContain('01');
    const link = root().querySelector<HTMLAnchorElement>('a[href="/atlas/dotnet/clr-memory"]')!;
    expect(link.textContent).toContain('Memory & GC');
    expect(link.textContent).toContain('3 concepts');
    expect(text()).toContain('Async & threading');
    expect(root().querySelector('a[href="/atlas/dotnet/async"]')).toBeNull();
    expect(text()).not.toContain('Old topic');
  });

  it('shows "not found" with a link back to the Atlas for an unknown or deprecated domain', async () => {
    await open('old-domain');

    expect(root().querySelector('app-atlas-not-found')).not.toBeNull();
    expect(text()).toContain('Not found');
    expect(root().querySelector('a[href="/atlas"]')?.textContent).toContain('Back to the Atlas');
  });

  it('shows readiness on the domain and its topics', async () => {
    await open('dotnet');

    expect(root().querySelector('header app-readiness-bar')?.textContent).toContain('25%');
    const memory = root().querySelector('a[href="/atlas/dotnet/clr-memory"]')!;
    expect(memory.querySelector('app-readiness-bar')?.textContent).toContain('25%');
    const async = Array.from(root().querySelectorAll('li')).find((li) =>
      li.textContent?.includes('Async & threading'),
    )!;
    expect(async.textContent).not.toContain('%');
  });

  it('links to a review of the whole domain', async () => {
    await open('dotnet');

    const link = linkTo('/review?scope=domain&domain=dotnet');
    expect(link.textContent).toContain('Review this domain');
  });
});
