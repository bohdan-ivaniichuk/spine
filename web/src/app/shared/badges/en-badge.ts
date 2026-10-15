import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18n } from '../i18n/i18n';

/**
 * "EN" marker for an item served in English while Ukrainian is selected (per-item fallback, FR-006).
 * Renders nothing in English or when the item is translated; the host takes no box (`contents`).
 */
@Component({
  selector: 'app-en-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  template: `
    @if (visible()) {
      @let t = i18n.t().badges;
      <span
        class="inline-flex shrink-0 items-center rounded-full border border-current px-2 py-0.5 align-middle font-mono text-[11px] leading-4 font-bold"
        lang="en"
        [attr.title]="t.fallbackHint"
        ><span aria-hidden="true">{{ t.fallback }}</span
        ><span class="sr-only" [attr.lang]="i18n.lang()">{{ t.fallbackHint }}</span></span
      >
    }
  `,
})
export class EnBadge {
  protected readonly i18n = inject(I18n);

  /** True when the item was requested in a non-English language and served in English. */
  readonly fallback = input.required<boolean>();

  protected readonly visible = computed(() => this.i18n.lang() === 'uk' && this.fallback());
}
