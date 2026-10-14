import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  viewChildren,
} from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { PromptMarkdown } from '../prompt-markdown/prompt-markdown';

export type OptionState = 'neutral' | 'selected' | 'correct' | 'wrong';

const KEY_HINTS = ['A', 'B', 'C', 'D'];

/** After Check the hint letter becomes a mark, so correct/wrong never rely on color alone (WCAG 1.4.1). */
const STATE_MARKS: Partial<Record<OptionState, string>> = { correct: '✓', wrong: '✗' };

const ROW_CLASSES: Record<OptionState, string> = {
  neutral:
    'border-transparent bg-surface-2 text-muted enabled:hover:border-line enabled:hover:text-text',
  selected: 'border-accent bg-accent-soft text-text',
  correct: 'border-ok-fill bg-ok-soft text-text',
  wrong: 'border-bad-fill bg-bad-soft text-text',
};

const HINT_CLASSES: Record<OptionState, string> = {
  neutral: 'bg-surface text-text',
  selected: 'bg-accent text-on-accent',
  correct: 'bg-ok-fill text-on-ok-fill',
  wrong: 'bg-bad-fill text-on-bad-fill',
};

/**
 * The four answer rows: a radiogroup of buttons with A–D key hints and token-only state colors.
 * Follows the ARIA radio-group pattern: one Tab stop (the selected row, else the first), arrows move
 * focus and selection.
 */
@Component({
  selector: 'app-option-list',
  imports: [PromptMarkdown],
  templateUrl: './option-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OptionList {
  protected readonly i18n = inject(I18n);
  readonly options = input.required<string[]>();
  readonly selected = input<number | null>(null);
  /** Revealed after Check; `null` while the answer is still open. */
  readonly correctIndex = input<number | null>(null);
  readonly locked = input(false);

  readonly selectedChange = output<number>();

  private readonly rows = viewChildren<ElementRef<HTMLButtonElement>>('row');

  protected hint(index: number): string {
    return STATE_MARKS[this.state(index)] ?? KEY_HINTS[index] ?? String(index + 1);
  }

  protected tabIndex(index: number): number {
    const selected = this.selected();
    return index === (selected ?? 0) ? 0 : -1;
  }

  protected onArrow(event: KeyboardEvent, index: number): void {
    const step =
      event.key === 'ArrowDown' || event.key === 'ArrowRight'
        ? 1
        : event.key === 'ArrowUp' || event.key === 'ArrowLeft'
          ? -1
          : 0;
    if (step === 0 || this.locked()) {
      return;
    }
    event.preventDefault();
    const count = this.options().length;
    const target = (index + step + count) % count;
    this.selectedChange.emit(target);
    this.rows()[target]?.nativeElement.focus();
  }

  protected state(index: number): OptionState {
    const correct = this.correctIndex();
    const selected = this.selected();
    if (correct !== null) {
      if (index === correct) {
        return 'correct';
      }
      return index === selected ? 'wrong' : 'neutral';
    }
    return index === selected ? 'selected' : 'neutral';
  }

  protected rowClasses(index: number): string {
    return ROW_CLASSES[this.state(index)];
  }

  protected hintClasses(index: number): string {
    return HINT_CLASSES[this.state(index)];
  }

  protected pick(index: number): void {
    if (!this.locked()) {
      this.selectedChange.emit(index);
    }
  }
}
