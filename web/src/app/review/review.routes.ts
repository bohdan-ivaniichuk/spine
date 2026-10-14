import { Routes } from '@angular/router';
import { ReviewOverview } from './review-overview/review-overview';
import { ReviewSession } from './review-session/review-session';
import { SessionSummary } from './session-summary/session-summary';

/** Route titles are `Messages.titles` keys (I18nTitleStrategy). `/review` (overview), `/review/session` (cards), `/review/summary` — lazy-loaded from app.routes.ts. */
export const REVIEW_ROUTES: Routes = [
  { path: '', component: ReviewOverview, title: 'review' },
  { path: 'session', component: ReviewSession, title: 'session' },
  { path: 'summary', component: SessionSummary, title: 'summary' },
];
