import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18n } from '../../shared/i18n/i18n';
import { titleDetail } from '../../shared/i18n/i18n-title.strategy';
import { KnowledgeTreeSource } from '../api/knowledge-tree.source';
import { domainConceptCount, pad } from '../atlas-view';
import { liveConcepts, liveTopics } from '../api/knowledge-tree.models';
import { AtlasStatus } from '../atlas-status/atlas-status';
import { EnBadge } from '../../shared/badges/en-badge';
import { ReadinessBar } from '../readiness/readiness-bar';
import { ReadinessStore } from '../readiness/readiness-store';
import { AtlasNotFound } from '../not-found/not-found';

/** `/atlas/:domain`: the domain's live topics with concept counts; "soon" ones are not links. */
@Component({
  selector: 'app-atlas-domain',
  imports: [RouterLink, AtlasStatus, AtlasNotFound, EnBadge, ReadinessBar],
  templateUrl: './atlas-domain.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AtlasDomain {
  protected readonly i18n = inject(I18n);
  private readonly source = inject(KnowledgeTreeSource);
  protected readonly readiness = inject(ReadinessStore);

  /** Route param (component input binding). */
  readonly domain = input.required<string>();

  protected readonly loaded = computed(() => this.source.current() !== undefined);

  /** `null` once the tree is loaded and the slug is unknown or deprecated. */
  protected readonly view = computed(() => {
    const d = this.source.domains().find((x) => x.slug === this.domain());
    if (!d) {
      return null;
    }
    return {
      ...d,
      number: pad(d.position),
      conceptCount: domainConceptCount(d),
      topics: liveTopics(d).map((t) => ({ ...t, conceptCount: liveConcepts(t).length })),
    };
  });

  constructor() {
    titleDetail(() => this.view()?.title);
  }
}
