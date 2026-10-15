import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { ReadinessSummary } from '../api/readiness.models';

/**
 * Readiness of a domain or topic: a bar in the domain colour and the percentage. With nothing to
 * count (`percent: null`) there is no bar and no number (not "0%"), only a screen-reader note.
 * Renders nothing until readiness is known.
 */
@Component({
  selector: 'app-readiness-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    @if (readiness(); as r) {
      @let t = i18n.t().atlas;
      @if (r.percent !== null) {
        <div class="flex items-center gap-2.5">
          <span
            class="flex h-1.5 min-w-0 flex-1 overflow-hidden rounded-full bg-surface-2"
            aria-hidden="true"
          >
            <span
              data-testid="readiness-fill"
              class="rounded-full"
              [class]="'domain-bar-' + position()"
              [style.width.%]="r.percent"
            ></span>
          </span>
          <span class="shrink-0 font-mono text-xs text-muted" aria-hidden="true"
            >{{ r.percent }}%</span
          >
          <span class="sr-only">{{ t.readinessValue(r.percent, r.ready, r.counted) }}</span>
        </div>
      } @else {
        <span class="sr-only">{{ t.readinessNone }}</span>
      }
    }
  `,
})
export class ReadinessBar {
  protected readonly i18n = inject(I18n);

  readonly readiness = input<ReadinessSummary | undefined>();
  /** 1-based manifest position of the domain (bar colour). */
  readonly position = input(1);
}
