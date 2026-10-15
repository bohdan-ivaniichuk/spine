import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';
import { renderNote } from './note-markdown';

/**
 * A concept note rendered from Markdown. The HTML comes from `renderNote` (raw HTML escaped) and is
 * bound with `[innerHTML]`, so Angular's sanitizer still applies — never `bypassSecurityTrust*`.
 */
@Component({
  selector: 'app-concept-note',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block min-w-0', '[attr.lang]': 'lang()' },
  template: `<div class="note" [innerHTML]="html()"></div>`,
})
export class ConceptNote {
  private readonly i18n = inject(I18n);

  readonly markdown = input.required<string>();
  /** Language the note is written in (may differ from the UI language on fallback). */
  readonly lang = input<'en' | 'uk'>('en');

  protected readonly html = computed(() => {
    const t = this.i18n.t().atlas;
    return renderNote(this.markdown(), {
      code: t.codeLanguage,
      table: t.table,
      newTab: t.opensInNewTab,
    });
  });
}
