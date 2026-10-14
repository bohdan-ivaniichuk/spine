import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { KnowledgeConcept, conceptUrl } from '../../knowledge/api/knowledge-concept.models';
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
    @if (conceptResource.hasValue()) {
      @let c = conceptResource.value();
      <div class="text-sm text-text">
        <p class="text-xs text-muted">
          {{ c.domain.title }} · {{ c.topic.title }} · level {{ c.level }} ·
          {{ c.questionCount }} questions
        </p>
        @if (note(); as text) {
          <app-prompt-markdown [source]="text" />
        } @else {
          <p class="mt-2 text-muted">No note for this concept yet.</p>
        }
      </div>
    } @else if (conceptResource.error()) {
      <p class="text-sm font-medium text-bad" role="alert">Could not load the concept.</p>
    } @else {
      <p class="text-sm text-muted">Loading…</p>
    }
  `,
})
export class ConceptPeek {
  readonly domain = input.required<string>();
  readonly topic = input.required<string>();
  readonly concept = input.required<string>();

  protected readonly conceptResource = httpResource<KnowledgeConcept>(() =>
    conceptUrl(this.domain(), this.topic(), this.concept()),
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
