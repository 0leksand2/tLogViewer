import { Injectable } from '@angular/core';
import { TranslateLoader, type TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import en from '../../../assets/i18n/en.json';
import uk from '../../../assets/i18n/uk.json';

const TRANSLATIONS: Record<string, TranslationObject> = {
  en: en as TranslationObject,
  uk: uk as TranslationObject,
};

/** Loads EN/UK catalogs from the bundle (no runtime HTTP). */
@Injectable()
export class AppTranslateLoader implements TranslateLoader {
  getTranslation(lang: string): Observable<TranslationObject> {
    return of(TRANSLATIONS[lang] ?? TRANSLATIONS['en']!);
  }
}
