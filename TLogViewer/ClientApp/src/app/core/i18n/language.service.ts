import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export type AppLanguage = 'en' | 'uk';

export const APP_LANGUAGES: readonly { value: AppLanguage; labelKey: string }[] = [
  { value: 'en', labelKey: 'settings.language.en' },
  { value: 'uk', labelKey: 'settings.language.uk' },
] as const;

const STORAGE_KEY = 'tlog-viewer.language';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  /** Current UI language; read in computeds so labels refresh on switch. */
  readonly lang = signal<AppLanguage>(this.readStored());

  init(): Promise<unknown> {
    const lang = this.lang();
    this.translate.addLangs(['en', 'uk']);
    this.translate.setFallbackLang('en');
    this.applyDocumentLang(lang);
    return firstValueFrom(this.translate.use(lang));
  }

  setLanguage(lang: AppLanguage): void {
    if (lang !== 'en' && lang !== 'uk') {
      return;
    }
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
    this.applyDocumentLang(lang);
    void firstValueFrom(this.translate.use(lang));
  }

  private readStored(): AppLanguage {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw === 'en' || raw === 'uk') {
        return raw;
      }
    } catch {
      // ignore
    }
    return 'en';
  }

  private applyDocumentLang(lang: AppLanguage): void {
    document.documentElement.lang = lang;
  }
}
