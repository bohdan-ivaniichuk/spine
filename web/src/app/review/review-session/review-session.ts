import {
  ChangeDetectionStrategy,
  DOCUMENT,
  Component,
  ElementRef,
  OnInit,
  afterRenderEffect,
  inject,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { FeedbackPanel } from '../feedback-panel/feedback-panel';
import { OptionList } from '../option-list/option-list';
import { ProgressBar } from '../progress-bar/progress-bar';
import { QuestionCard } from '../question-card/question-card';
import { OVERVIEW_ROUTE, SessionStore } from '../session-store';

const DIGIT_KEYS = ['1', '2', '3', '4'];
const LETTER_KEYS = ['a', 'b', 'c', 'd'];

/** One question at a time: progress, card, options, feedback, Check / Next / End session. */
@Component({
  selector: 'app-review-session',
  imports: [ProgressBar, QuestionCard, OptionList, FeedbackPanel],
  templateUrl: './review-session.html',
  host: { '(document:keydown)': 'onKeydown($event)' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewSession implements OnInit {
  protected readonly store = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly feedbackPanel = viewChild(FeedbackPanel, { read: ElementRef<HTMLElement> });
  private readonly card = viewChild<ElementRef<HTMLElement>>('card');
  /** Id of the question whose feedback holds focus; cleared once focus moves to the next card. */
  private checkedQuestionId: string | null = null;

  constructor() {
    // The Check button is replaced by Next and the radios get disabled, so focus would fall to
    // <body>; move it to the feedback so keyboard users land on the verdict. After Next the
    // feedback empties, so focus moves on to the new question card for it to be announced.
    afterRenderEffect(() => {
      const status = this.store.status();
      const questionId = this.store.current()?.id ?? null;
      // Only on the first 'checked' per question: a failed finish returns to 'checked' and must
      // leave focus near the error alert.
      if (status === 'checked' && questionId !== this.checkedQuestionId) {
        this.checkedQuestionId = questionId;
        this.feedbackPanel()?.nativeElement.focus();
      } else if (
        status === 'ready' &&
        this.checkedQuestionId !== null &&
        questionId !== this.checkedQuestionId
      ) {
        this.checkedQuestionId = null;
        this.card()?.nativeElement.focus();
      }
    });
  }

  ngOnInit(): void {
    void this.load();
  }

  /** Resumes the open session; also the Retry action of the load-error state. */
  protected async load(): Promise<void> {
    const session = await this.store.resume();
    if (!session) {
      if (this.store.status() !== 'error') {
        await this.router.navigateByUrl(OVERVIEW_ROUTE);
      }
      return;
    }
    if (!this.store.current()) {
      // Everything already answered (or skipped): nothing left to show, go straight to the summary.
      await this.store.finish();
    }
  }

  protected check(): void {
    void this.store.check();
  }

  protected next(): void {
    this.store.next();
  }

  protected endEarly(): void {
    const answered = this.store.answeredCount();
    const total = this.store.total();
    const confirmed =
      this.document.defaultView?.confirm(
        `End the session now? ${answered} of ${total} answered; the summary will reflect those.`,
      ) ?? true;
    if (confirmed) {
      void this.store.finish();
    }
  }

  /** 1–4 (or A–D) select while unchecked (arrows are handled by the option list); Enter checks, then Enter moves on. Inputs and modifier chords are ignored. */
  protected onKeydown(event: KeyboardEvent): void {
    // A held key auto-repeats; without the repeat guard one long Enter would Check and then skip the feedback.
    if (event.repeat || event.altKey || event.ctrlKey || event.metaKey || event.isComposing) {
      return;
    }
    if (isEditable(event.target)) {
      return;
    }
    const status = this.store.status();
    const key = event.key.toLowerCase();
    const optionIndex =
      DIGIT_KEYS.indexOf(key) >= 0 ? DIGIT_KEYS.indexOf(key) : LETTER_KEYS.indexOf(key);
    if (optionIndex >= 0) {
      if (status === 'ready') {
        event.preventDefault();
        this.store.select(optionIndex);
      }
      return;
    }
    if (event.key === 'Enter') {
      // A focused link or button keeps its native Enter behaviour; only the already-checked radio
      // (or nothing focusable) lets Enter act as Check / Next.
      const target = event.target;
      if (
        target instanceof HTMLElement &&
        target.closest('a, button') &&
        !target.matches('[role=radio][aria-checked="true"]')
      ) {
        return;
      }
      if (status === 'ready' && this.store.selectedIndex() !== null) {
        event.preventDefault();
        this.check();
      } else if (status === 'checked') {
        event.preventDefault();
        this.next();
      }
    }
  }
}

function isEditable(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }
  return (
    target.isContentEditable ||
    target.tagName === 'INPUT' ||
    target.tagName === 'TEXTAREA' ||
    target.tagName === 'SELECT'
  );
}
