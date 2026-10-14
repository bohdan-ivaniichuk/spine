import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';

export type ThemePreference = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'spine.theme';
/** Key used before the rename to Spine; read once and migrated. */
const LEGACY_KEY = 'interviewprep.theme';

/**
 * Atlas light and Atlas dark (ADR 0006).
 * `system` leaves <html> without data-theme so the CSS follows prefers-color-scheme.
 */
@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly document = inject(DOCUMENT);

  readonly preference = signal<ThemePreference>(this.read());

  /** The theme actually shown, resolving `system` against the OS preference. */
  readonly effective = computed<'light' | 'dark'>(() => {
    const pref = this.preference();
    if (pref !== 'system') {
      return pref;
    }
    return this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
  });

  constructor() {
    effect(() => {
      const pref = this.preference();
      const root = this.document.documentElement;
      if (pref === 'system') {
        root.removeAttribute('data-theme');
      } else {
        root.setAttribute('data-theme', pref);
      }
      this.write(pref);
    });
  }

  toggle(): void {
    this.preference.set(this.effective() === 'dark' ? 'light' : 'dark');
  }

  private read(): ThemePreference {
    try {
      const storage = this.document.defaultView?.localStorage;
      const legacy = storage?.getItem(LEGACY_KEY);
      if (legacy && storage) {
        storage.setItem(STORAGE_KEY, legacy);
        storage.removeItem(LEGACY_KEY);
      }
      const raw = storage?.getItem(STORAGE_KEY);
      return raw === 'light' || raw === 'dark' ? raw : 'system';
    } catch {
      return 'system';
    }
  }

  private write(pref: ThemePreference): void {
    try {
      const storage = this.document.defaultView?.localStorage;
      if (!storage) {
        return;
      }
      if (pref === 'system') {
        storage.removeItem(STORAGE_KEY);
      } else {
        storage.setItem(STORAGE_KEY, pref);
      }
    } catch {
      /* private mode or blocked storage: the preference simply does not persist */
    }
  }
}
