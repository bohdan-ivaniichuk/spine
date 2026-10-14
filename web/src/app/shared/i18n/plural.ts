export type Lang = 'en' | 'uk';

/** Plural forms keyed by CLDR category; `other` is required, the rest fall back to it. */
export type PluralForms = Partial<Record<Intl.LDMLPluralRule, string>> & { other: string };

const RULES: Record<Lang, Intl.PluralRules> = {
  en: new Intl.PluralRules('en'),
  uk: new Intl.PluralRules('uk'),
};

/** `plural('uk', 5, { one: 'питання', few: 'питання', many: 'питань', other: 'питання' })` → "питань". */
export function plural(lang: Lang, count: number, forms: PluralForms): string {
  return forms[RULES[lang].select(count)] ?? forms.other;
}
