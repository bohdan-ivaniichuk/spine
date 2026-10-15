import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { KnowledgeTreeSource } from '../../knowledge/api/knowledge-tree.source';
import { EnBadge } from '../../shared/badges/en-badge';
import { UnreviewedBadge } from '../../shared/badges/unreviewed-badge';
import { I18n } from '../../shared/i18n/i18n';
import { SessionQuestion } from '../api/review.models';
import { PromptMarkdown } from '../prompt-markdown/prompt-markdown';

/** Card header (domain chip in its color, topic → concept, level/new/EN/unreviewed badges) and the prompt. */
@Component({
  selector: 'app-question-card',
  imports: [PromptMarkdown, EnBadge, UnreviewedBadge],
  templateUrl: './question-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestionCard {
  protected readonly i18n = inject(I18n);
  private readonly domains = inject(KnowledgeTreeSource).domains;

  readonly question = input.required<SessionQuestion>();

  protected readonly levelName = computed(
    () => this.i18n.t().session.levels[this.question().level] ?? this.question().level,
  );

  /** Manifest position of the question's domain (1–5) for its color; 1 until the tree loads. */
  protected readonly domainPosition = computed(
    () => this.domains().find((d) => d.slug === this.question().domain.slug)?.position ?? 1,
  );
}
