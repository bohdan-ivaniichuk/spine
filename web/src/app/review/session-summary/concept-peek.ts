import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { KnowledgeConcept, conceptUrl } from '../../knowledge/api/knowledge-concept.models';
import { I18n, withLang } from '../../shared/i18n/i18n';
import { PromptMarkdown } from '../prompt-markdown/prompt-markdown';

/**
 * A weak concept's note shown in place on the summary, until feature 003 routes concept pages.
 * The prompt Markdown subset renders fences and inline code; heading markers and bold asterisks are
 * stripped so the note reads cleanly without a full Markdown renderer.
 */
@Component({
  selector: 'app-concept-peek',
  imports: [PromptMarkdown],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @let t = i18n.t().summary;
    @if (conceptResource.hasValue()) {
      @let c = conceptResource.value();
      <div class="text-sm text-text" [attr.lang]="c.language">
        <p class="text-xs text-muted">
          {{ t.conceptMeta(c.domain.title, c.topic.title, c.level, c.questionCount) }}
        </p>
        @if (note(); as text) {
          <app-prompt-markdown [source]="text" />
        } @else {
          <p class="mt-2 text-muted">{{ t.noNote }}</p>
        }
      </div>
    } @else if (conceptResource.error()) {
      <p class="text-sm font-medium text-bad" role="alert">{{ t.conceptError }}</p>
    } @else {
      <p class="text-sm text-muted">{{ t.loading }}</p>
    }
  `,
})
export class ConceptPeek {
  protected readonly i18n = inject(I18n);
  readonly domain = input.required<string>();
  readonly topic = input.required<string>();
  readonly concept = input.required<string>();

  protected readonly conceptResource = httpResource<KnowledgeConcept>(() =>
    withLang(conceptUrl(this.domain(), this.topic(), this.concept()), this.i18n.lang()),
  );

  protected readonly note = computed(() => {
    const note = this.conceptResource.hasValue() ? this.conceptResource.value().note : null;
    return note ? plainNote(note) : null;
  });
}

function plainNote(markdown: string): string {
  let inFence = false;
  return markdown
    .split('\n')
    .map((line) => {
      if (line.trimStart().startsWith('```')) {
        inFence = !inFence;
        return line;
      }
      return inFence ? line : stripEmphasis(line.replace(/^#{1,6}\s+/, ''));
    })
    .join('\n');
}

/** Drops `**bold**` and `*italic*` markers outside inline code spans (odd backtick segments). */
function stripEmphasis(line: string): string {
  return line
    .split('`')
    .map((part, i) =>
      i % 2 === 1 ? part : part.replaceAll('**', '').replace(/\*([^*\s][^*]*?)\*/g, '$1'),
    )
    .join('`');
}
