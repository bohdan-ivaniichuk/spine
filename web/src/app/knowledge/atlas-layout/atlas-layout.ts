import { ViewportScroller } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DOCUMENT,
  ElementRef,
  computed,
  inject,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRouteSnapshot,
  NavigationEnd,
  Params,
  Router,
  RouterLink,
  RouterOutlet,
} from '@angular/router';
import { filter, map } from 'rxjs';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { liveConcepts, liveTopics } from '../api/knowledge-tree.models';
import { EnBadge } from '../../shared/badges/en-badge';
import { ReadinessStore } from '../readiness/readiness-store';

interface Crumb {
  label: string;
  /** Router link; `null` for the current (last) level. */
  link: string[] | null;
  fallback: boolean;
}

/**
 * Shell of the Atlas section: breadcrumbs Atlas › domain › topic › concept, built from the URL and
 * the shared knowledge tree, above the routed level. Entering the Atlas re-fetches the tree so a
 * re-import shows up on the next visit; it also provides the per-visit `ReadinessStore`.
 */
@Component({
  selector: 'app-atlas-layout',
  imports: [RouterOutlet, RouterLink, EnBadge],
  // One readiness load per Atlas visit, shared by every level below.
  providers: [ReadinessStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './atlas-layout.html',
})
export class AtlasLayout {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly top = viewChild.required<ElementRef<HTMLElement>>('top');

  /** Slugs of the deepest activated route (the layout's own inputs never see child params). */
  private readonly params = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => deepestParams(this.router.routerState.snapshot.root)),
    ),
    { initialValue: deepestParams(this.router.routerState.snapshot.root) },
  );

  protected readonly crumbs = computed<Crumb[]>(() => {
    const { domain, topic, concept } = this.params() as Record<string, string | undefined>;
    const crumbs: Crumb[] = [
      { label: this.i18n.t().atlas.title, link: ['/atlas'], fallback: false },
    ];
    const d = domain ? this.source.domains().find((x) => x.slug === domain) : undefined;
    if (d) {
      crumbs.push({ label: d.title, link: ['/atlas', d.slug], fallback: d.titleFallback });
      const tp = topic ? liveTopics(d).find((x) => x.slug === topic) : undefined;
      if (tp) {
        crumbs.push({
          label: tp.title,
          link: ['/atlas', d.slug, tp.slug],
          fallback: tp.titleFallback,
        });
        const c = concept ? liveConcepts(tp).find((x) => x.slug === concept) : undefined;
        if (c) {
          crumbs.push({
            label: c.title,
            link: ['/atlas', d.slug, tp.slug, c.slug],
            fallback: c.titleFallback,
          });
        }
      }
    }
    crumbs[crumbs.length - 1] = { ...crumbs[crumbs.length - 1], link: null };
    return crumbs;
  });

  constructor() {
    // A fresh tree per Atlas visit (the root resource is otherwise cached for the app's lifetime).
    this.source.tree.reload();

    // Moving to another Atlas page (e.g. "next concept" at the bottom of a note) starts at the top.
    const scroller = inject(ViewportScroller);
    let path = this.path();
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        const next = this.path();
        if (next !== path) {
          path = next;
          scroller.scrollToPosition([0, 0]);
          // Keyboard and screen-reader users start at the new page too, not on the clicked link
          // (which is gone). `#main` is the shell's skip target; the layout root is the fallback.
          const target = this.document.getElementById('main') ?? this.top().nativeElement;
          target.focus({ preventScroll: true });
        }
      });
  }

  private path(): string {
    return this.router.url.split(/[?#]/)[0];
  }
}

function deepestParams(root: ActivatedRouteSnapshot): Params {
  const params: Params = { ...root.params };
  let route = root;
  while (route.firstChild) {
    route = route.firstChild;
    Object.assign(params, route.params);
  }
  return params;
}
