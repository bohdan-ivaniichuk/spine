import { DOCUMENT, Injectable, computed, effect, inject, signal } from '@angular/core';
import { EN, Messages } from './messages.en';
import { UK } from './messages.uk';
import type { Lang } from './plural';

export { plural } from './plural';
export type { Lang, PluralForms } from './plural';

export const LANGS: readonly Lang[] = ['en', 'uk'];

const STORAGE_KEY = 'spine.lang';

const MESSAGES: Record<Lang, Messages> = { en: EN, uk: UK };

/**
 * UI language (EN canonical, UK second — constitution III). Runtime switch without a reload: the
 * dictionaries are typed objects, `t()` is a signal, so templates re-render on change. The same
 * language is sent to the API (`?lang=`) so content follows, with per-item English fallback.
 */
@Injectable({ providedIn: 'root' })
export class I18n {
  private readonly document = inject(DOCUMENT);

  readonly lang = signal<Lang>(this.read());

  /** The active dictionary: `i18n.t().review.start`. */
  readonly t = computed<Messages>(() => MESSAGES[this.lang()]);

  /** BCP 47 locale for Intl formatting. */
  readonly locale = computed(() => (this.lang() === 'uk' ? 'uk-UA' : 'en-GB'));

  constructor() {
    effect(() => {
      this.document.documentElement.lang = this.lang();
    });
  }

  /** An explicit choice; persisted (a browser-derived default is not, so it can follow the OS). */
  set(lang: Lang): void {
    this.lang.set(lang);
    this.write(lang);
  }

  /** Short weekday + day + month, e.g. "Wed 15 Oct" / "ср, 15 жовт.". */
  formatDay(value: Date | string): string {
    return new Intl.DateTimeFormat(this.locale(), {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
    }).format(new Date(value));
  }

  formatDateTime(value: Date | string): string {
    return new Intl.DateTimeFormat(this.locale(), {
      day: 'numeric',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  formatPercent(value: number): string {
    return new Intl.NumberFormat(this.locale(), {
      style: 'percent',
      maximumFractionDigits: 0,
    }).format(value);
  }

  private read(): Lang {
    try {
      const raw = this.document.defaultView?.localStorage?.getItem(STORAGE_KEY);
      if (raw === 'en' || raw === 'uk') {
        return raw;
      }
    } catch {
      /* blocked storage: fall through to the browser language */
    }
    const browser = this.document.defaultView?.navigator?.language ?? 'en';
    return browser.toLowerCase().startsWith('uk') ? 'uk' : 'en';
  }

  private write(lang: Lang): void {
    try {
      this.document.defaultView?.localStorage?.setItem(STORAGE_KEY, lang);
    } catch {
      /* private mode: the choice simply does not persist */
    }
  }
}

/** Appends `lang` to an API URL when it is not the server default (`en`), keeping English URLs unchanged. */
export function withLang(url: string, lang: Lang): string {
  if (lang === 'en') {
    return url;
  }
  return `${url}${url.includes('?') ? '&' : '?'}lang=${lang}`;
}
