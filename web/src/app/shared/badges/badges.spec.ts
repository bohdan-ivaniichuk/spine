import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { I18n } from '../i18n/i18n';
import { EnBadge } from './en-badge';
import { UnreviewedBadge } from './unreviewed-badge';

@Component({
  imports: [EnBadge, UnreviewedBadge],
  template: `<app-en-badge [fallback]="fallback()" /><app-unreviewed-badge
      [reviewed]="reviewed()"
    />`,
})
class Host {
  readonly fallback = signal(true);
  readonly reviewed = signal(false);
}

describe('badges', () => {
  beforeEach(() => localStorage.removeItem('spine.lang'));
  afterEach(() => TestBed.inject(I18n).set('en'));

  async function render(fallback: boolean, reviewed: boolean) {
    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.fallback.set(fallback);
    fixture.componentInstance.reviewed.set(reviewed);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    return {
      fixture,
      en: () => root.querySelector('app-en-badge')!.textContent?.trim() ?? '',
      unreviewed: () => root.querySelector('app-unreviewed-badge')!.textContent?.trim() ?? '',
      root,
    };
  }

  it('EnBadge renders only when the language is uk and the item is a fallback', async () => {
    TestBed.inject(I18n).set('en');
    const english = await render(true, true);
    expect(english.en()).toBe('');

    TestBed.inject(I18n).set('uk');
    await english.fixture.whenStable();
    expect(english.en()).toContain('EN');
    expect(english.root.querySelector('app-en-badge [lang=en]')).not.toBeNull();
    expect(english.root.querySelector('app-en-badge [title]')?.getAttribute('title')).toContain(
      'Українського перекладу',
    );

    english.fixture.componentInstance.fallback.set(false);
    await english.fixture.whenStable();
    expect(english.en()).toBe('');
  });

  it('UnreviewedBadge renders for unreviewed items in both languages and never for reviewed ones', async () => {
    const view = await render(false, false);
    expect(view.unreviewed()).toBe('unreviewed');

    TestBed.inject(I18n).set('uk');
    await view.fixture.whenStable();
    expect(view.unreviewed()).toBe('не перевірено');

    view.fixture.componentInstance.reviewed.set(true);
    await view.fixture.whenStable();
    expect(view.unreviewed()).toBe('');
  });
});
