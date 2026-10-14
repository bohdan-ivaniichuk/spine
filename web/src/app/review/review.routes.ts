import { Routes } from '@angular/router';
import { ReviewOverview } from './review-overview/review-overview';
import { ReviewSession } from './review-session/review-session';
import { SessionSummary } from './session-summary/session-summary';

/** `/review` (overview), `/review/session` (cards), `/review/summary` — lazy-loaded from app.routes.ts. */
export const REVIEW_ROUTES: Routes = [
  { path: '', component: ReviewOverview, title: 'Review · InterviewPrep' },
  { path: 'session', component: ReviewSession, title: 'Review session · InterviewPrep' },
  { path: 'summary', component: SessionSummary, title: 'Session summary · InterviewPrep' },
];
