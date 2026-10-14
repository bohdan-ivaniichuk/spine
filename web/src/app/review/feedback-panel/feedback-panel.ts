import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { AnswerFeedback } from '../api/review.models';
import { PromptMarkdown } from '../prompt-markdown/prompt-markdown';

/**
 * Result of a checked answer. The host element carries `aria-live` and is always in the DOM, so the
 * verdict is announced when the feedback arrives; it is focusable (`tabindex="-1"`) so the session
 * page can move focus here after Check.
 */
@Component({
  selector: 'app-feedback-panel',
  imports: [PromptMarkdown],
  templateUrl: './feedback-panel.html',
  host: { 'aria-live': 'polite', class: 'block outline-none', tabindex: '-1' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeedbackPanel {
  protected readonly i18n = inject(I18n);
  readonly feedback = input<AnswerFeedback | null>(null);
}
