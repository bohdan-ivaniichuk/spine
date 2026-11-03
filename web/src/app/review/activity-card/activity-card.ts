import { httpResource } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  ElementRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { Activity, activityUrl } from '../api/activity.models';

/**
 * The overview's streak card (feature 007): current and longest streak in the browser's time zone,
 * a nudge while the streak is alive but today has no answer yet, an empty state and an error state
 * that leave the rest of the overview alone. Fetched fresh on every visit (component-scoped resource).
 */
@Component({
  selector: 'app-activity-card',
  templateUrl: './activity-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityCard {
  protected readonly i18n = inject(I18n);
  private readonly document = inject(DOCUMENT);
  private readonly heading = viewChild.required<ElementRef<HTMLElement>>('heading');

  /** Set by Retry: the error branch (and its focused button) stays until the reload settles. */
  private readonly retrying = signal(false);

  /** Language-independent payload, so no `?lang=`; the zone is read once per card. */
  private readonly url = activityUrl();
  protected readonly activity = httpResource<Activity>(() => this.url);

  protected readonly showError = computed(
    () => !!this.activity.error() || (this.retrying() && this.activity.isLoading()),
  );

  protected readonly data = computed(() =>
    this.activity.hasValue() ? this.activity.value() : null,
  );

  /** Nothing ever answered: the card invites a first review instead of showing zeros. */
  protected readonly empty = computed(() => {
    const a = this.data();
    return a !== null && a.longestStreak === 0;
  });

  protected readonly keepStreak = computed(() => {
    const a = this.data();
    return a !== null && a.currentStreak > 0 && !a.activeToday;
  });

  /** One bar per day: height relative to the busiest day, a narrow weekday and a spoken label. */
  protected readonly bars = computed(() => {
    const days = this.data()?.days ?? [];
    const busiest = Math.max(1, ...days.map((d) => d.answers));
    const locale = this.i18n.locale();
    // `YYYY-MM-DD` parses as UTC midnight; format in UTC so a browser west of UTC keeps the date.
    const weekday = new Intl.DateTimeFormat(locale, { weekday: 'narrow', timeZone: 'UTC' });
    const spoken = new Intl.DateTimeFormat(locale, {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      timeZone: 'UTC',
    });
    return days.map((d) => {
      const date = new Date(`${d.date}T00:00:00Z`);
      return {
        ...d,
        // A day with answers never looks empty, however busy the busiest day was.
        height: `${d.answers > 0 ? Math.max(6, Math.round((d.answers / busiest) * 100)) : 0}%`,
        weekday: weekday.format(date),
        spoken: `${spoken.format(date)}: ${d.answers}`,
      };
    });
  });

  protected readonly total = computed(() => this.bars().reduce((sum, d) => sum + d.answers, 0));

  constructor() {
    // After a retry the button that had focus is gone; hand focus to the card's heading instead.
    effect(() => {
      if (this.retrying() && !this.activity.isLoading()) {
        this.retrying.set(false);
        if (!this.activity.error()) {
          this.heading().nativeElement.focus();
        }
      }
    });
  }

  protected retry(): void {
    this.retrying.set(true);
    this.activity.reload();
  }

  /** The empty state's call to action: the overview's start form, first control focused. */
  protected focusStart(): void {
    this.document
      .getElementById('review-start')
      ?.querySelector<HTMLElement>('button, select, input')
      ?.focus();
  }
}
