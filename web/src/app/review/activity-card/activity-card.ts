import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
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

  /** Language-independent payload, so no `?lang=`; the zone is read once per card. */
  private readonly url = activityUrl();
  protected readonly activity = httpResource<Activity>(() => this.url);

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
        height: `${Math.round((d.answers / busiest) * 100)}%`,
        weekday: weekday.format(date),
        spoken: `${spoken.format(date)}: ${d.answers}`,
      };
    });
  });

  protected readonly total = computed(() => this.bars().reduce((sum, d) => sum + d.answers, 0));

  protected retry(): void {
    this.activity.reload();
  }
}
