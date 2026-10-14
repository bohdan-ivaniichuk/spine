import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'review', pathMatch: 'full' },
  {
    path: 'review',
    loadChildren: () => import('./review/review.routes').then((m) => m.REVIEW_ROUTES),
  },
  // Knowledge (003) and katas (004) are not routed yet; unknown paths fall back to the review overview.
  { path: '**', redirectTo: 'review' },
];
