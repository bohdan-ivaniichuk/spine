import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { I18n } from './shared/i18n/i18n';
import { TREE, answerTree } from './review/testing/fixtures';

describe('App', () => {
  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  async function render() {
    const fixture = TestBed.createComponent(App);
    TestBed.tick();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/health').flush({ status: 'Healthy' });
    answerTree(http, TREE);
    await fixture.whenStable();
    return { fixture, http, root: fixture.nativeElement as HTMLElement };
  }

  it('renders the spine rail: brand, sections, domain tabs, language, theme and API status', async () => {
    const { root } = await render();

    expect(root.querySelector('a[aria-label="Spine home"]')).not.toBeNull();
    expect(root.querySelector('nav[aria-label="Main"]')?.textContent).toContain('Review');
    const tab = root.querySelector<HTMLAnchorElement>('nav[aria-label="Domains"] a')!;
    expect(tab.textContent?.trim()).toBe('01');
    expect(tab.getAttribute('href')).toBe('/review?scope=domain&domain=dotnet');
    expect(tab.className).toContain('domain-1');
    expect(root.querySelector('app-api-status')?.textContent).toContain('API: OK');
    expect(root.querySelector('app-theme-toggle button')).not.toBeNull();
    expect(root.querySelectorAll('app-language-toggle button')).toHaveLength(2);
  });

  it('switches the whole shell to Ukrainian and re-requests the tree in Ukrainian', async () => {
    const { fixture, http, root } = await render();

    root.querySelectorAll<HTMLButtonElement>('app-language-toggle button')[1].click();
    TestBed.tick();
    http.expectOne('/api/knowledge/tree?lang=uk').flush(TREE);
    await fixture.whenStable();

    expect(TestBed.inject(I18n).lang()).toBe('uk');
    expect(document.documentElement.lang).toBe('uk');
    expect(root.querySelector('nav[aria-label="Головне"]')?.textContent).toContain('Повторення');
    expect(root.querySelector('app-api-status')?.textContent).toContain('API: працює');
    TestBed.inject(I18n).set('en');
    TestBed.tick();
    answerTree(http);
  });
});
