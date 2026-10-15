import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';
import { titleDetail } from '../../shared/i18n/i18n-title.strategy';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { pad } from '../atlas-view';
import { liveConcepts, liveTopics } from '../api/knowledge-tree.models';
import { AtlasStatus } from '../atlas-status/atlas-status';
import { EnBadge } from '../../shared/badges/en-badge';
import { UnreviewedBadge } from '../../shared/badges/unreviewed-badge';
import { ReadinessBar } from '../readiness/readiness-bar';
import { ReadinessChip } from '../readiness/readiness-chip';
import { ReadinessStore } from '../readiness/readiness-store';
import { AtlasNotFound } from '../not-found/not-found';

/** `/atlas/:domain/:topic`: the topic's live concepts with level and question count. */
@Component({
  selector: 'app-atlas-topic',
  imports: [
    RouterLink,
    AtlasStatus,
    AtlasNotFound,
    EnBadge,
    UnreviewedBadge,
    ReadinessBar,
    ReadinessChip,
  ],
  templateUrl: './atlas-topic.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtlasTopic {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);
  protected readonly readiness = inject(ReadinessStore);

  /** Route params (component input binding). */
  readonly domain = input.required<string>();
  readonly topic = input.required<string>();

  protected readonly loaded = computed(() => this.source.current() !== undefined);

  /** `null` once the tree is loaded and the domain/topic pair is unknown or deprecated. */
  protected readonly view = computed(() => {
    const d = this.source.domains().find((x) => x.slug === this.domain());
    const topic = d && liveTopics(d).find((x) => x.slug === this.topic());
    if (!d || !topic) {
      return null;
    }
    return {
      domain: { ...d, number: pad(d.position) },
      topic,
      concepts: liveConcepts(topic),
    };
  });

  protected levelName(level: string): string {
    return this.i18n.t().session.levels[level] ?? level;
  }

  constructor() {
    titleDetail(() => this.view()?.topic.title);
  }
}
