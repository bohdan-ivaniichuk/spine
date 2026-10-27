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
  {
    path: 'weak',
    loadComponent: () => import('./review/weak-spots/weak-spots').then((m) => m.WeakSpots),
    title: 'weak',
  },
  // Katas are not routed yet; unknown top-level paths fall back to the review overview.
  { path: '**', redirectTo: 'review' },
];
