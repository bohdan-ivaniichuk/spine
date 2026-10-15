import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'review', pathMatch: 'full' },
  {
    path: 'review',
    loadChildren: () => import('./review/review.routes').then((m) => m.REVIEW_ROUTES),
  },
  {
    path: 'atlas',
    loadChildren: () => import('./knowledge/knowledge.routes').then((m) => m.KNOWLEDGE_ROUTES),
  },
  // Katas are not routed yet; unknown top-level paths fall back to the review overview.
  { path: '**', redirectTo: 'review' },
];
