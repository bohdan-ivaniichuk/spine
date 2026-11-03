import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { settle } from '../../knowledge/testing/atlas-fixtures';
import { I18n } from '../../shared/i18n/i18n';
import { Activity, activityUrl, browserTimeZone } from '../api/activity.models';
import { activity } from '../testing/fixtures';
import { ActivityCard } from './activity-card';

describe('activityUrl', () => {
  it('sends the given zone, encoded', () => {
    expect(activityUrl('America/Argentina/Buenos_Aires')).toBe(
      '/api/review/activity?tz=America%2FArgentina%2FBuenos_Aires',
    );
  });

  it('omits tz when there is no zone, so the API uses UTC', () => {
    expect(activityUrl('')).toBe('/api/review/activity');
  });

  it("defaults to the browser's zone", () => {
    expect(activityUrl()).toBe(activityUrl(browserTimeZone()));
    const spy = vi
      .spyOn(Intl.DateTimeFormat.prototype, 'resolvedOptions')
      .mockReturnValue({ timeZone: 'Europe/Kyiv' } as Intl.ResolvedDateTimeFormatOptions);
    try {
      expect(browserTimeZone()).toBe('Europe/Kyiv');
    } finally {
      spy.mockRestore();
    }
  });
});

describe('ActivityCard', () => {
  let fixture: ComponentFixture<ActivityCard>;
  let http: HttpTestingController;
  const root = () => fixture.nativeElement as HTMLElement;
  const text = () => (root().textContent ?? '').replace(/\s+/g, ' ');

  beforeEach(async () => {
    localStorage.removeItem('spine.lang');
    await TestBed.configureTestingModule({
      imports: [ActivityCard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    TestBed.inject(I18n).set('en');
    http.verify();
  });

  async function open(body: Activity | 'error' = activity()) {
    fixture = TestBed.createComponent(ActivityCard);
    TestBed.tick();
    const request = http.expectOne(activityUrl());
    if (body === 'error') {
      request.flush(null, { status: 500, statusText: 'Server Error' });
    } else {
      request.flush(body);
    }
    await settle(fixture);
  }

  it('is quiet (no live region) and shows a loading line until activity arrives', async () => {
    fixture = TestBed.createComponent(ActivityCard);
    TestBed.tick();
    expect(root().querySelector('[aria-live]')).toBeNull();
    expect(text()).toContain('Loading activity…');
    http.expectOne(activityUrl()).flush(activity());
    await settle(fixture);
    expect(root().querySelector('[data-testid=activity-loading]')).toBeNull();
  });

  it("requests activity in the browser's time zone", async () => {
    await open();
    // expectOne(activityUrl()) above is the assertion; the card shows the figures.
    expect(text()).toContain('4');
  });

  it('shows the current and the longest streak with plurals', async () => {
    await open(activity({ currentStreak: 1, longestStreak: 9 }));

    expect(root().querySelector('[data-testid=current-streak]')?.textContent).toMatch(
      /1\s+day in a row/,
    );
    expect(root().querySelector('[data-testid=longest-streak]')?.textContent).toMatch(
      /Longest:\s+9 days/,
    );
  });

  it('uses Ukrainian plurals', async () => {
    TestBed.inject(I18n).set('uk');
    await open(activity({ currentStreak: 5, longestStreak: 2 }));

    expect(root().querySelector('[data-testid=current-streak]')?.textContent).toMatch(
      /5\s+днів поспіль/,
    );
    expect(root().querySelector('[data-testid=longest-streak]')?.textContent).toMatch(
      /Найдовша:\s+2 дні/,
    );
  });

  it('asks for an answer today only while the streak is alive and today is inactive', async () => {
    await open(activity({ currentStreak: 3, activeToday: false }));
    expect(root().querySelector('[data-testid=keep-streak]')).not.toBeNull();

    fixture.destroy();
    await open(activity({ currentStreak: 3, activeToday: true }));
    expect(root().querySelector('[data-testid=keep-streak]')).toBeNull();

    fixture.destroy();
    await open(activity({ currentStreak: 0, activeToday: false, longestStreak: 4 }));
    expect(root().querySelector('[data-testid=keep-streak]')).toBeNull();
  });

  it('invites a first review when there is no activity at all', async () => {
    await open(activity({ currentStreak: 0, activeToday: false, longestStreak: 0 }));

    const empty = root().querySelector('[data-testid=activity-empty]');
    expect(empty?.textContent).toContain('No streak yet');
    expect(root().querySelector('[data-testid=current-streak]')).toBeNull();

    // The call to action focuses the overview's start form (no fragment link: the app has a base href).
    const form = document.createElement('form');
    form.id = 'review-start';
    const first = document.createElement('button');
    form.append(first);
    document.body.append(form);
    try {
      root().querySelector<HTMLButtonElement>('[data-testid=activity-start]')!.click();
      expect(document.activeElement).toBe(first);
    } finally {
      form.remove();
    }
  });

  it('shows an error with a retry when activity cannot be loaded', async () => {
    await open('error');

    expect(text()).toContain('Activity could not be loaded.');
    expect(root().querySelector('[role=status]')?.textContent).toContain('Activity could not be loaded.');
    const retry = root().querySelector<HTMLButtonElement>('[data-testid=activity-retry]')!;
    retry.click();
    TestBed.tick();
    // While reloading, the focused button stays in place, disabled.
    expect(root().querySelector('[data-testid=activity-retry]')).toBe(retry);
    expect(retry.disabled).toBe(true);
    http.expectOne(activityUrl()).flush(activity());
    await settle(fixture);
    expect(root().querySelector('[data-testid=current-streak]')).not.toBeNull();
    expect(document.activeElement?.id).toBe('activity-h');
  });

  /** 14 days ending 3 Nov 2026 (a Tuesday); the busiest day has 8 answers. */
  function twoWeeks(counts: number[] = [0, 2, 0, 0, 4, 0, 0, 0, 1, 0, 0, 0, 8, 3]) {
    return counts.map((answers, i) => {
      const date = new Date(Date.UTC(2026, 9, 21 + i));
      return { date: date.toISOString().slice(0, 10), answers };
    });
  }

  it('draws 14 bars, oldest first, scaled to the busiest day', async () => {
    await open(activity({ days: twoWeeks() }));

    const bars = Array.from(root().querySelectorAll<HTMLElement>('[data-testid=activity-bar]'));
    expect(bars).toHaveLength(14);
    expect(bars[12].style.height).toBe('100%');
    expect(bars[4].style.height).toBe('50%');
    expect(bars[8].style.height).toBe('13%');
    expect(bars[0].dataset['answers']).toBe('0');
    expect(bars[13].dataset['answers']).toBe('3');
    // The chart is decoration for sighted users; the text alternative carries the figures.
    expect(root().querySelector('[data-testid=activity-chart]')?.getAttribute('aria-hidden')).toBe('true');
  });

  it('gives the chart a text alternative with the total and each day', async () => {
    await open(activity({ days: twoWeeks() }));

    const alt = root().querySelector('[data-testid=activity-chart-text]')!;
    expect(alt.classList).toContain('sr-only');
    const said = alt.textContent!.replace(/\s+/g, ' ');
    expect(said).toContain('Last 14 days: 18 answers.');
    expect(said).toContain('Wed 21 Oct: 0');
    expect(said).toContain('Tue 3 Nov: 3');
  });

  it('labels weekdays in the selected language, on the calendar date of each day', async () => {
    await open(activity({ days: twoWeeks() }));
    const labels = () =>
      Array.from(root().querySelectorAll('[data-testid=activity-weekday]')).map((l) => l.textContent?.trim());
    expect(labels()[0]).toBe('W');
    expect(labels()[13]).toBe('T');

    TestBed.inject(I18n).set('uk');
    await settle(fixture);
    expect(labels()[0]).toBe('С');
    expect(root().querySelector('[data-testid=activity-chart-text]')?.textContent).toContain('Останні 14 днів: 18 відповідей.');
  });
});
