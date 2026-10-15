import { Routes } from '@angular/router';
import { AtlasDomain } from './atlas-domain/atlas-domain';
import { AtlasHome } from './atlas-home/atlas-home';
import { AtlasLayout } from './atlas-layout/atlas-layout';
import { AtlasTopic } from './atlas-topic/atlas-topic';

/**
 * `/atlas` → domain → topic → concept (feature 004), lazy-loaded from app.routes.ts. Slugs reach the
 * pages as inputs (`withComponentInputBinding`). Route titles are `Messages.titles` keys.
 */
export const KNOWLEDGE_ROUTES: Routes = [
  {
    path: '',
    component: AtlasLayout,
    children: [
      { path: '', component: AtlasHome, title: 'atlas' },
      { path: ':domain', component: AtlasDomain, title: 'atlas' },
      { path: ':domain/:topic', component: AtlasTopic, title: 'atlas' },
      {
        // Lazy on its own so markdown-it and highlight.js load only with the first note.
        path: ':domain/:topic/:concept',
        loadComponent: () => import('./concept-page/concept-page').then((m) => m.ConceptPage),
        title: 'atlas',
      },
    ],
  },
];
