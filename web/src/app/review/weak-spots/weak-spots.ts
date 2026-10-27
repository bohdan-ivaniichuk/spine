import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { KnowledgeTreeSource } from '../../knowledge/api/knowledge-tree.source';
import { ReadinessChip } from '../../knowledge/readiness/readiness-chip';
import { EnBadge } from '../../shared/badges/en-badge';
import { I18n } from '../../shared/i18n/i18n';
import {
  WEAK_SPOTS_URL,
  WeakConceptStats,
  WeakSpots as WeakSpotsDto,
  WeakTopicStats,
} from '../api/weak-spots.models';

/** A title from the knowledge tree; `fallback` = English served while Ukrainian is selected. */
interface Title {
  text: string;
  fallback: boolean;
}

export interface WeakConceptRow extends WeakConceptStats {
  title: Title;
  domainTitle: Title;
  topicTitle: Title;
  /** `/review` query params of the weak drill of this concept. */
  drill: Record<string, string>;
}

export interface WeakTopicRow extends WeakTopicStats {
  title: Title;
  domainTitle: Title;
  /** `/review` query params of the weak drill of this topic. */
  drill: Record<string, string>;
}

/**
 * `/weak`: the learner's weakest concepts and topics (FR-007…FR-011, FR-015) with a note link and a
 * one-click weak drill each. The statistics are fetched fresh on every visit (component-scoped
 * resource) and carry slugs only; titles come from the shared knowledge tree in the UI language.
 */
@Component({
  selector: 'app-weak-spots',
  imports: [RouterLink, EnBadge, ReadinessChip],
  templateUrl: './weak-spots.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeakSpots {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);

  /** Language-independent (FR-012), so no `?lang=`. */
  protected readonly stats = httpResource<WeakSpotsDto>(() => WEAK_SPOTS_URL);

  /** `domain`, `domain/topic` and `domain/topic/concept` → title (deprecated items included). */
  private readonly titles = computed(() => {
    const index = new Map<string, Title>();
    for (const d of this.source.current()?.domains ?? []) {
      index.set(d.slug, { text: d.title, fallback: d.titleFallback });
      for (const t of d.topics) {
        index.set(`${d.slug}/${t.slug}`, { text: t.title, fallback: t.titleFallback });
        for (const c of t.concepts) {
          index.set(`${d.slug}/${t.slug}/${c.slug}`, { text: c.title, fallback: c.titleFallback });
        }
      }
    }
    return index;
  });

  protected readonly data = computed(() => (this.stats.hasValue() ? this.stats.value() : null));

  protected readonly concepts = computed<WeakConceptRow[]>(() =>
    (this.data()?.concepts ?? []).map((c) => ({
      ...c,
      title: this.title(`${c.domain}/${c.topic}/${c.concept}`, c.concept),
      domainTitle: this.title(c.domain, c.domain),
      topicTitle: this.title(`${c.domain}/${c.topic}`, c.topic),
      drill: {
        mode: 'weak',
        scope: 'concept',
        domain: c.domain,
        topic: c.topic,
        concept: c.concept,
      },
    })),
  );

  protected readonly topics = computed<WeakTopicRow[]>(() =>
    (this.data()?.topics ?? []).map((t) => ({
      ...t,
      title: this.title(`${t.domain}/${t.topic}`, t.topic),
      domainTitle: this.title(t.domain, t.domain),
      drill: { mode: 'weak', scope: 'topic', domain: t.domain, topic: t.topic },
    })),
  );

  /** The slug stands in until the tree is loaded (or for an item it no longer has). */
  private title(key: string, slug: string): Title {
    return this.titles().get(key) ?? { text: slug, fallback: false };
  }
}
