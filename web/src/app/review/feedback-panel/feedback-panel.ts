import { ChangeDetectionStrategy, Component, input } from '@angular/core';
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
  readonly feedback = input<AnswerFeedback | null>(null);
}
