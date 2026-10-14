import { Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { I18n } from './i18n';

/**
 * Route `title` values are keys of `Messages.titles` ("review", "session", …). The document title is
 * "<page> · Spine" in the active language and follows language switches without navigation.
 */
@Injectable({ providedIn: 'root' })
export class I18nTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly i18n = inject(I18n);
  private readonly key = signal<string | undefined>(undefined);

  constructor() {
    super();
    effect(() => {
      const t = this.i18n.t();
      const key = this.key();
      const page = key ? (t.titles as Record<string, string>)[key] : undefined;
      this.title.setTitle(page ? `${page} · ${t.app.name}` : t.app.name);
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.key.set(this.buildTitle(snapshot));
  }
}
