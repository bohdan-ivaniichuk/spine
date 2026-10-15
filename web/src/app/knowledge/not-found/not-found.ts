import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';

/** Unknown or deprecated Atlas path (FR-015): a calm dead end with the way back. */
@Component({
  selector: 'app-atlas-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let t = i18n.t().atlas;
    <section class="flex flex-col items-start gap-4 rounded-card bg-surface p-7 shadow-card">
      <p class="font-mono text-sm text-muted" aria-hidden="true">404</p>
      <h1 class="font-display text-2xl font-bold tracking-[-0.03em] text-text">
        {{ t.notFound }}
      </h1>
      <p class="max-w-[38rem] text-muted">{{ t.notFoundLead }}</p>
      <a
        routerLink="/atlas"
        class="rounded-2xl bg-accent px-5 py-2.5 text-sm font-semibold text-on-accent hover:opacity-90 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >{{ t.back }}</a
      >
    </section>
  `,
})
export class AtlasNotFound {
  protected readonly i18n = inject(I18n);
}
