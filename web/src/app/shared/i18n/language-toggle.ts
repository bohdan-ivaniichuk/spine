import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { I18n, LANGS, Lang } from './i18n';

const LABELS: Record<Lang, string> = { en: 'EN', uk: 'УК' };
const NAMES: Record<Lang, string> = { en: 'English', uk: 'Українська' };

/** EN / УК segmented switch on the ink rail: stacked on the vertical rail (lg), side by side in the bar. */
@Component({
  selector: 'app-language-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      role="group"
      class="flex shrink-0 rounded-2xl bg-ink-2 p-[3px] text-xs font-bold lg:flex-col"
      [attr.aria-label]="i18n.t().app.language"
    >
      @for (lang of langs; track lang) {
        <button
          type="button"
          class="rounded-[13px] px-2.5 py-2 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-on-ink"
          [class]="i18n.lang() === lang ? 'bg-on-ink text-ink' : 'text-ink-muted hover:text-on-ink'"
          [attr.aria-pressed]="i18n.lang() === lang"
          [attr.lang]="lang"
          [attr.title]="names[lang]"
          (click)="i18n.set(lang)"
        >
          {{ labels[lang] }}
        </button>
      }
    </div>
  `,
})
export class LanguageToggle {
  protected readonly i18n = inject(I18n);

  protected readonly langs = LANGS;
  protected readonly labels = LABELS;
  protected readonly names = NAMES;
}
