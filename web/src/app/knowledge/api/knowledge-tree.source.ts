import { httpResource } from '@angular/common/http';
import { Injectable, computed, inject, linkedSignal } from '@angular/core';
import { I18n, withLang } from '../../shared/i18n/i18n';
import { TreeDomain, KnowledgeTree } from './knowledge-tree.models';

/**
 * The knowledge tree in the active language, fetched once and shared by the spine rail and the
 * review overview; re-fetched when the language changes.
 */
@Injectable({ providedIn: 'root' })
export class KnowledgeTreeSource {
  private readonly i18n = inject(I18n);

  readonly tree = httpResource<KnowledgeTree>(() =>
    withLang('/api/knowledge/tree', this.i18n.lang()),
  );

  /**
   * The last loaded tree. While a language switch re-fetches, the resource has no value; keeping the
   * previous one stops the rail and the atlas from blanking out.
   */
  readonly current = linkedSignal<KnowledgeTree | undefined, KnowledgeTree | undefined>({
    source: () => (this.tree.hasValue() ? this.tree.value() : undefined),
    computation: (tree, previous) => tree ?? previous?.value,
  });

  /** Live domains in manifest order, each with its 1-based position (drives the domain colors). */
  readonly domains = computed<(TreeDomain & { position: number })[]>(() =>
    (this.current()?.domains ?? [])
      .filter((d) => !d.deprecatedAt)
      .map((d, i) => ({ ...d, position: (i % 5) + 1 })),
  );
}
