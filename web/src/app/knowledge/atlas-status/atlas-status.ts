import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';

/** Loading / error state of the knowledge tree for an Atlas page that has nothing to show yet. */
@Component({
  selector: 'app-atlas-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let t = i18n.t();
    @if (source.tree.error()) {
      <div class="flex flex-wrap items-center gap-3" role="alert">
        <p class="font-medium text-bad">{{ t.atlas.loadError }}</p>
        <button
          type="button"
          class="rounded-xl bg-surface px-3 py-1.5 text-sm font-medium text-text shadow-card hover:text-accent-text focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          (click)="source.tree.reload()"
        >
          {{ t.app.retry }}
        </button>
      </div>
    } @else {
      <p class="text-muted" aria-live="polite">{{ t.atlas.loading }}</p>
    }
  `,
})
export class AtlasStatus {
  protected readonly i18n = inject(I18n);
  protected readonly source = inject(KnowledgeTreeSource);
}
