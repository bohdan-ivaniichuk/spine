import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { KnowledgeTreeSource } from './knowledge/api/knowledge-tree.source';
import { ApiStatus } from './shared/api-status/api-status';
import { I18n } from './shared/i18n/i18n';
import { LanguageToggle } from './shared/i18n/language-toggle';
import { ThemeToggle } from './shared/theme/theme-toggle';

/** Shell: the "spine" rail (sections, domain tabs 01–05, language, theme, API status) + the page. */
@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ApiStatus, ThemeToggle, LanguageToggle],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly i18n = inject(I18n);
  protected readonly domains = inject(KnowledgeTreeSource).domains;

  protected pad(position: number): string {
    return String(position).padStart(2, '0');
  }
}
