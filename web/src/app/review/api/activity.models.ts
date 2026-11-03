/**
 * `GET /api/review/activity` (`specs/007-activity-streak/contracts/activity-api.md`): the local
 * learner's streaks and last 14 days of answers, with days in the requested time zone. No text in the
 * payload, so no `?lang=`.
 */

/** Answers on one calendar day (`YYYY-MM-DD` in the requested zone). */
export interface ActivityDay {
  date: string;
  answers: number;
}

export interface Activity {
  /** Resolved IANA zone id; `UTC` when none was sent. */
  timeZone: string;
  /** Today in that zone, `YYYY-MM-DD`. */
  today: string;
  /** Run ending today, or ending yesterday while `activeToday` is false. */
  currentStreak: number;
  activeToday: boolean;
  longestStreak: number;
  /** 14 days, oldest first, ending today. */
  days: ActivityDay[];
}

export const ACTIVITY_URL = '/api/review/activity';

/** The browser's IANA time zone, or `undefined` when the runtime does not report one. */
export function browserTimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || undefined;
  } catch {
    return undefined;
  }
}

/** Activity in the given zone (the browser's by default); without one the API uses UTC. */
export function activityUrl(timeZone: string | undefined = browserTimeZone()): string {
  return timeZone ? `${ACTIVITY_URL}?tz=${encodeURIComponent(timeZone)}` : ACTIVITY_URL;
}
