import { PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { scopeLabel } from '../api/review.models';
import { OVERVIEW_ROUTE, SessionStore } from '../session-store';

/** How the session went: counts, accuracy, weak concepts ranked by misses, due tomorrow. */
@Component({
  selector: 'app-session-summary',
  imports: [RouterLink, PercentPipe],
  templateUrl: './session-summary.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SessionSummary implements OnInit {
  protected readonly store = inject(SessionStore);
  private readonly router = inject(Router);

  protected readonly summary = this.store.summary;

  protected readonly scope = computed(() => {
    const summary = this.summary();
    return summary ? scopeLabel(summary.scope) : '';
  });

  ngOnInit(): void {
    // The summary lives only in memory (a reload goes back through the API on the overview).
    if (!this.summary()) {
      void this.router.navigateByUrl(OVERVIEW_ROUTE);
    }
  }

  protected reviewAgain(): void {
    void this.store.reviewAgain();
  }
}
