import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';
import { scopeLabel } from '../api/review.models';
import { OVERVIEW_ROUTE, SessionStore } from '../session-store';
import { ConceptPeek } from './concept-peek';

/** How the session went: counts, accuracy, weak concepts ranked by misses, due tomorrow. */
@Component({
  selector: 'app-session-summary',
  imports: [RouterLink, ConceptPeek],
  templateUrl: './session-summary.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionSummary implements OnInit {
  protected readonly store = inject(SessionStore);
  protected readonly i18n = inject(I18n);
  private readonly router = inject(Router);

  protected readonly summary = this.store.summary;
  /** `domain/topic/concept` of the weak concept whose note is open (one at a time). */
  protected readonly expanded = signal<string | null>(null);

  protected readonly scope = computed(() => {
    const summary = this.summary();
    return summary ? scopeLabel(summary.scope, this.i18n.t().scope.all) : '';
  });

  ngOnInit(): void {
    // The summary lives only in memory (a reload goes back through the API on the overview).
    if (!this.summary()) {
      void this.router.navigateByUrl(OVERVIEW_ROUTE);
    }
  }

  protected toggle(key: string): void {
    this.expanded.update((open) => (open === key ? null : key));
  }

  protected reviewAgain(): void {
    void this.store.reviewAgain();
  }
}
