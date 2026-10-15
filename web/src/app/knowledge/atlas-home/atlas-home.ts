import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { domainConceptCount, pad } from '../atlas-view';
import { liveConcepts, liveTopics } from '../api/knowledge-tree.models';
import { AtlasStatus } from '../atlas-status/atlas-status';
import { EnBadge } from '../../shared/badges/en-badge';
import { ReadinessBar } from '../readiness/readiness-bar';
import { ReadinessStore } from '../readiness/readiness-store';

/** `/atlas`: every live domain in manifest order with its topics (FR-001). */
@Component({
  selector: 'app-atlas-home',
  imports: [RouterLink, AtlasStatus, EnBadge, ReadinessBar],
  templateUrl: './atlas-home.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtlasHome {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);
  protected readonly readiness = inject(ReadinessStore);

  protected readonly loaded = computed(() => this.source.current() !== undefined);

  protected readonly domains = computed(() =>
    this.source.domains().map((d) => ({
      ...d,
      number: pad(d.position),
      conceptCount: domainConceptCount(d),
      topics: liveTopics(d).map((t) => ({ ...t, conceptCount: liveConcepts(t).length })),
    })),
  );
}
