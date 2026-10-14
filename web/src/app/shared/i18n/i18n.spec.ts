import { TestBed } from '@angular/core/testing';
import { I18n, plural, withLang } from './i18n';
import { EN } from './messages.en';
import { UK } from './messages.uk';

describe('I18n', () => {
  beforeEach(() => localStorage.clear());

  it('defaults to English, persists the choice and sets <html lang>', () => {
    const i18n = TestBed.inject(I18n);
    expect(i18n.lang()).toBe('en');

    i18n.set('uk');
    TestBed.tick();

    expect(i18n.t().app.review).toBe('Повторення');
    expect(document.documentElement.lang).toBe('uk');
    expect(localStorage.getItem('spine.lang')).toBe('uk');
    i18n.set('en');
    TestBed.tick();
  });

  it('restores a stored language', () => {
    localStorage.setItem('spine.lang', 'uk');
    expect(TestBed.inject(I18n).lang()).toBe('uk');
  });

  it('uses Ukrainian plural categories (1 / 2–4 / 5+ / 11–14)', () => {
    const cards = (n: number) => UK.overview.start(n);
    expect(cards(1)).toBe('Почати · 1 картка');
    expect(cards(3)).toBe('Почати · 3 картки');
    expect(cards(5)).toBe('Почати · 5 карток');
    expect(cards(11)).toBe('Почати · 11 карток');
    expect(cards(22)).toBe('Почати · 22 картки');
    expect(UK.feedback.nextIn(21)).toBe('Наступне повторення через 21 день');
    expect(EN.feedback.nextIn(1)).toBe('Next review in 1 day');
    expect(plural('en', 2, { one: 'card', other: 'cards' })).toBe('cards');
  });

  it('adds ?lang only for non-default languages', () => {
    expect(withLang('/api/review/coverage', 'en')).toBe('/api/review/coverage');
    expect(withLang('/api/review/coverage', 'uk')).toBe('/api/review/coverage?lang=uk');
    expect(withLang('/api/review/overview?scope=all', 'uk')).toBe(
      '/api/review/overview?scope=all&lang=uk',
    );
  });

  it('has a Ukrainian string for every English key', () => {
    const keys = (o: object, prefix = ''): string[] =>
      Object.entries(o).flatMap(([k, v]) =>
        v && typeof v === 'object' ? keys(v, `${prefix}${k}.`) : [`${prefix}${k}`],
      );
    expect(keys(UK).sort()).toEqual(keys(EN).sort());
  });
});
