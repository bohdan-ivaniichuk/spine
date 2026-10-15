import { HttpErrorResponse, httpResource } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18n, withLang } from '../../shared/i18n/i18n';
import { titleDetail } from '../../shared/i18n/i18n-title.strategy';
import { KnowledgeConcept, conceptUrl } from '../api/knowledge-concept.models';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { liveConcepts, liveTopics } from '../api/knowledge-tree.models';
import { EnBadge } from '../../shared/badges/en-badge';
import { UnreviewedBadge } from '../../shared/badges/unreviewed-badge';
import { ConceptNote } from '../concept-note/concept-note';
import { AtlasNotFound } from '../not-found/not-found';
import { ReadinessChip } from '../readiness/readiness-chip';
import { ReadinessStore } from '../readiness/readiness-store';

/**
 * `/atlas/:domain/:topic/:concept`: the note, its meta (level, question and kata counts) and the
 * previous/next concept of the topic (FR-003, FR-005). Lazy-loaded with the Markdown renderer.
 */
@Component({
  selector: 'app-concept-page',
  imports: [RouterLink, ConceptNote, AtlasNotFound, EnBadge, UnreviewedBadge, ReadinessChip],
  templateUrl: './concept-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConceptPage {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);
  protected readonly readiness = inject(ReadinessStore);

  /** Route params (component input binding). */
  readonly domain = input.required<string>();
  readonly topic = input.required<string>();
  readonly concept = input.required<string>();

  /** Keyed on the slugs and the language: a switch re-requests, the last request wins. */
  protected readonly resource = httpResource<KnowledgeConcept>(() =>
    withLang(conceptUrl(this.domain(), this.topic(), this.concept()), this.i18n.lang()),
  );

  private readonly key = computed(() => `${this.domain()}/${this.topic()}/${this.concept()}`);

  /**
   * The loaded concept. While a language switch re-fetches the same concept, the previous value is
   * kept (no flash); under new slugs it is not — the page shows "loading" instead of the old note.
   */
  protected readonly page = linkedSignal<
    { key: string; value: KnowledgeConcept | undefined },
    KnowledgeConcept | undefined
  >({
    source: () => ({
      key: this.key(),
      value: this.resource.hasValue() ? this.resource.value() : undefined,
    }),
    computation: (source, previous) =>
      source.value ?? (previous && previous.source.key === source.key ? previous.value : undefined),
  });

  protected readonly state = computed<'ready' | 'loading' | 'not-found' | 'error'>(() => {
    const error = this.resource.error();
    if (error) {
      return error instanceof HttpErrorResponse && error.status === 404 ? 'not-found' : 'error';
    }
    const page = this.page();
    if (!page) {
      return 'loading';
    }
    return page.deprecatedAt ? 'not-found' : 'ready';
  });

  /** Markdown or `null` when the concept has no note yet. */
  protected readonly note = computed(() => this.page()?.note?.trim() || null);

  /** Live concepts of the topic in sort order, from the shared tree. */
  private readonly siblings = computed(() => {
    const d = this.source.domains().find((x) => x.slug === this.domain());
    const topic = d && liveTopics(d).find((x) => x.slug === this.topic());
    return topic ? liveConcepts(topic) : [];
  });

  protected readonly neighbours = computed(() => {
    const list = this.siblings();
    const index = list.findIndex((c) => c.slug === this.concept());
    return {
      previous: index > 0 ? list[index - 1] : null,
      next: index >= 0 && index < list.length - 1 ? list[index + 1] : null,
    };
  });

  constructor() {
    titleDetail(() => (this.state() === 'ready' ? this.page()?.title : null));
  }

  protected levelName(level: string): string {
    return this.i18n.t().session.levels[level] ?? level;
  }
}
