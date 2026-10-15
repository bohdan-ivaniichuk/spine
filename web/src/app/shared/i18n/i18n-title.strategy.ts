import { DestroyRef, Injectable, effect, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { I18n } from './i18n';

/**
 * Route `title` values are keys of `Messages.titles` ("review", "session", …). The document title is
 * "<page> · Spine" in the active language and follows language switches without navigation. A page
 * showing a named item (an Atlas domain, topic or concept) adds it in front via `titleDetail()`:
 * "GC modes · Atlas · Spine".
 */
@Injectable({ providedIn: 'root' })
export class I18nTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly i18n = inject(I18n);
  private readonly key = signal<string | undefined>(undefined);

  /** Item-specific prefix set by the routed page; cleared when that page is destroyed. */
  readonly detail = signal<string | null>(null);

  constructor() {
    super();
    effect(() => {
      const t = this.i18n.t();
      const key = this.key();
      const page = key ? (t.titles as Record<string, string>)[key] : undefined;
      this.title.setTitle([this.detail(), page, t.app.name].filter(Boolean).join(' · '));
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.key.set(this.buildTitle(snapshot));
  }
}

/**
 * Call in a routed component's injection context: keeps the document title's item prefix in sync
 * with `source` (e.g. the concept title in the active language) and drops it on destroy.
 */
export function titleDetail(source: () => string | null | undefined): void {
  const strategy = inject(I18nTitleStrategy);
  effect(() => strategy.detail.set(source() || null));
  inject(DestroyRef).onDestroy(() => strategy.detail.set(null));
}
