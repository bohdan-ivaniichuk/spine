import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { ConceptReadiness, ReadinessState } from '../api/readiness.models';

const STYLES: Record<ReadinessState, string> = {
  ready: 'bg-ok-fill text-on-ok-fill',
  'in-progress': 'bg-ok-soft text-ok',
  'not-started': 'border border-line text-muted',
};

/** A concept's readiness state; nothing for a concept without schedulable questions. */
@Component({
  selector: 'app-readiness-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'contents' },
  template: `
    @if (readiness(); as r) {
      @let t = i18n.t().atlas;
      <span
        class="inline-flex h-6 shrink-0 items-center rounded-full px-2.5 text-xs font-medium"
        [class]="style()"
        [attr.title]="t.stateHint(r.readyQuestions, r.questions)"
        >{{ t.state[r.state]
        }}<span class="sr-only">: {{ t.stateHint(r.readyQuestions, r.questions) }}</span></span
      >
    }
  `,
})
export class ReadinessChip {
  protected readonly i18n = inject(I18n);

  readonly readiness = input<ConceptReadiness | undefined>();

  protected readonly style = computed(() => {
    const r = this.readiness();
    return r ? STYLES[r.state] : '';
  });
}
