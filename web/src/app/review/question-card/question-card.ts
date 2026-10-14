import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { QuestionLevel, SessionQuestion } from '../api/review.models';
import { PromptMarkdown } from '../prompt-markdown/prompt-markdown';

const LEVEL_NAMES: Record<QuestionLevel, string> = {
  M: 'Middle',
  S: 'Senior',
  L: 'Lead',
};

/** Card header (level badge, unreviewed/new badges, scope breadcrumb) and the prompt. */
@Component({
  selector: 'app-question-card',
  imports: [PromptMarkdown],
  templateUrl: './question-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestionCard {
  readonly question = input.required<SessionQuestion>();

  protected readonly levelName = computed(
    () => LEVEL_NAMES[this.question().level] ?? this.question().level,
  );
}
