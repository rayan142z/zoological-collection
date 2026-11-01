import { Injectable, signal } from '@angular/core';
import { EN } from './en';

export type Lang = 'de' | 'en';
const STORAGE_KEY = 'zoolog_lang';

/**
 * Minimal DE/EN translation. German text is the key; EN holds the English
 * version. Missing entries fall back to German, so untranslated text still shows.
 */
@Injectable({ providedIn: 'root' })
export class I18n {
  readonly lang = signal<Lang>(this.initialLang());

  constructor() {
    document.documentElement.lang = this.lang();
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
    document.documentElement.lang = lang;
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // storage blocked: language resets on reload
    }
  }

  toggle(): void {
    this.setLang(this.lang() === 'de' ? 'en' : 'de');
  }

  /** Translates a German string; {name} placeholders are filled from params. */
  t(de: string, params?: Record<string, string | number>): string {
    let text = this.lang() === 'en' ? (EN[de] ?? de) : de;
    if (params) {
      for (const [k, v] of Object.entries(params)) text = text.replace(`{${k}}`, String(v));
    }
    return text;
  }

  /** Locale for date pipes and number formatting. */
  locale(): string {
    return this.lang() === 'en' ? 'en-GB' : 'de-DE';
  }

  private initialLang(): Lang {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'de' || stored === 'en') return stored;
    } catch {
      // ignore
    }
    return 'de';
  }
}
