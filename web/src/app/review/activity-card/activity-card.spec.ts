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
    expect(empty?.querySelector('a')?.getAttribute('href')).toBe('#review-start');
    expect(root().querySelector('[data-testid=current-streak]')).toBeNull();
  });

  it('shows an error with a retry when activity cannot be loaded', async () => {
    await open('error');

    expect(text()).toContain('Activity could not be loaded.');
    root().querySelector<HTMLButtonElement>('[data-testid=activity-retry]')!.click();
    TestBed.tick();
    http.expectOne(activityUrl()).flush(activity());
    await settle(fixture);
    expect(root().querySelector('[data-testid=current-streak]')).not.toBeNull();
  });
});
