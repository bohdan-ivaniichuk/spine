import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { I18n } from '../i18n/i18n';

/** "unreviewed" marker for content not yet human-reviewed (FR-007), in either language. */
@Component({
  selector: 'app-unreviewed-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  template: `
    @if (!reviewed()) {
      @let t = i18n.t().badges;
      <span
        class="inline-flex shrink-0 items-center rounded-full bg-warn-soft px-2.5 py-0.5 align-middle text-xs leading-5 font-medium text-warn"
        [attr.title]="t.unreviewedHint"
        >{{ t.unreviewed }}</span
      >
    }
  `,
})
export class UnreviewedBadge {
  protected readonly i18n = inject(I18n);

  readonly reviewed = input.required<boolean>();
}
