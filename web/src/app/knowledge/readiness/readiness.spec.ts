import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { I18n } from '../../shared/i18n/i18n';
import { ConceptReadiness, ReadinessSummary } from '../api/readiness.models';
import { READINESS, flushReadiness } from '../testing/atlas-fixtures';
import { ReadinessBar } from './readiness-bar';
import { ReadinessChip } from './readiness-chip';
import { ReadinessStore } from './readiness-store';

@Component({
  imports: [ReadinessBar, ReadinessChip],
  template: `<app-readiness-bar [readiness]="summary()" [position]="2" /><app-readiness-chip
      [readiness]="concept()"
    />`,
})
class Host {
  readonly summary = signal<ReadinessSummary | undefined>(undefined);
  readonly concept = signal<ConceptReadiness | undefined>(undefined);
}

describe('readiness', () => {
  beforeEach(() => {
    localStorage.removeItem('spine.lang');
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ReadinessStore],
    });
  });
  afterEach(() => TestBed.inject(I18n).set('en'));

  async function render(summary?: ReadinessSummary, concept?: ConceptReadiness) {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.summary.set(summary);
    fixture.componentInstance.concept.set(concept);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    return {
      fixture,
      bar: () => root.querySelector('app-readiness-bar')!,
      chip: () => root.querySelector('app-readiness-chip')!,
    };
  }

  it('ReadinessBar draws the percentage and an accessible description', async () => {
    const { bar } = await render({ ready: 1, counted: 4, percent: 25 });

    expect(bar().textContent).toContain('25%');
    const fill = bar().querySelector<HTMLElement>('[data-testid=readiness-fill]')!;
    expect(fill.style.width).toBe('25%');
    expect(fill.className).toContain('domain-bar-2');
    expect(bar().querySelector('.sr-only')?.textContent).toContain('1 of 4');
  });

  it('ReadinessBar shows no number and no bar when percent is null, only a screen-reader note', async () => {
    const { bar } = await render({ ready: 0, counted: 0, percent: null });

    expect(bar().textContent).not.toContain('%');
    expect(bar().querySelector('[data-testid=readiness-fill]')).toBeNull();
    expect(bar().querySelector('.sr-only')?.textContent).toContain('No readiness yet');
  });

  it('ReadinessBar renders nothing before readiness is known', async () => {
    const { bar } = await render();

    expect(bar().textContent?.trim()).toBe('');
  });

  it('ReadinessChip names the concept state in either language', async () => {
    const view = await render(undefined, {
      concept: 'gc-modes',
      state: 'in-progress',
      questions: 6,
      readyQuestions: 2,
    });
    expect(view.chip().textContent?.split(':')[0].trim()).toBe('in progress');
    expect(view.chip().querySelector('[title]')?.getAttribute('title')).toContain('2 of 6');
    // The hint is also spoken, not only a hover tooltip.
    expect(view.chip().querySelector('.sr-only')?.textContent).toContain('2 of 6 questions ready');

    TestBed.inject(I18n).set('uk');
    await view.fixture.whenStable();
    expect(view.chip().textContent?.split(':')[0].trim()).toBe('у процесі');
    expect(view.chip().querySelector('.sr-only')?.textContent).toContain('готові 2 з 6 питань');
  });

  it('ReadinessChip renders nothing for a concept without schedulable questions', async () => {
    const { chip } = await render(undefined, undefined);

    expect(chip().textContent?.trim()).toBe('');
  });

  it('ReadinessStore loads /api/review/readiness without lang and looks items up by slug', async () => {
    TestBed.inject(I18n).set('uk');
    const store = TestBed.inject(ReadinessStore);
    const http = TestBed.inject(HttpTestingController);
    TestBed.tick();
    http.expectOne('/api/review/readiness').flush(READINESS);
    await new Promise((resolve) => setTimeout(resolve));

    expect(store.domain('dotnet')?.percent).toBe(25);
    expect(store.topic('dotnet', 'async')?.percent).toBeNull();
    expect(store.concept('dotnet', 'clr-memory', 'gc-modes')?.state).toBe('in-progress');
    expect(store.concept('dotnet', 'clr-memory', 'nope')).toBeUndefined();
    flushReadiness(http);
    http.verify();
  });
});
