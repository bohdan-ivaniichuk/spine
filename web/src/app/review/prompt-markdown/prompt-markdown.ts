import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';
import { Inline, parsePromptMarkdown } from './prompt-markdown.parser';

/**
 * Renders the prompt Markdown subset (fenced code + inline code) through interpolation only —
 * no `innerHTML`, so seed content can never inject markup. `inline` collapses everything into one
 * `<span>` (used for option rows).
 */
@Component({
  selector: 'app-prompt-markdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (inline()) {
      <span>
        @for (part of inlines(); track $index) {
          @if (part.kind === 'code') {
            <code class="rounded bg-surface-2 px-1 font-mono text-[0.92em]">{{ part.text }}</code>
          } @else {
            {{ part.text }}
          }
        }
      </span>
    } @else {
      @for (block of blocks(); track $index) {
        @if (block.kind === 'code') {
          <pre
            class="my-3 overflow-x-auto rounded-card bg-code-bg p-4 font-mono text-sm leading-relaxed text-code-text"
            [attr.data-language]="block.language || null"
          ><code>{{ block.code }}</code></pre>
        } @else {
          <p class="my-2 whitespace-pre-line leading-relaxed">
            @for (part of block.inlines; track $index) {
              @if (part.kind === 'code') {
                <code class="rounded bg-surface-2 px-1 font-mono text-[0.92em]">{{
                  part.text
                }}</code>
              } @else {
                {{ part.text }}
              }
            }
          </p>
        }
      }
    }
  `,
})
export class PromptMarkdown {
  readonly source = input.required<string>();
  readonly inline = input(false, { transform: booleanAttribute });

  protected readonly blocks = computed(() => parsePromptMarkdown(this.source()));

  protected readonly inlines = computed<Inline[]>(() =>
    this.blocks().flatMap((block) =>
      block.kind === 'code' ? [{ kind: 'code' as const, text: block.code }] : block.inlines,
    ),
  );
}
