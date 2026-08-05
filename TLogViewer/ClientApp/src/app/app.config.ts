import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { provideToastr } from 'ngx-toastr';
import { provideTranslateLoader, provideTranslateService } from '@ngx-translate/core';

import { routes } from './app.routes';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { LanguageService } from './core/i18n/language.service';
import { AppTranslateLoader } from './core/i18n/app-translate.loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimations(),
    provideHttpClient(withInterceptors([httpErrorInterceptor])),
    provideRouter(routes),
    provideToastr({
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      progressBar: true,
      newestOnTop: true,
    }),
    provideTranslateService({
      lang: 'en',
      fallbackLang: 'en',
      loader: provideTranslateLoader(AppTranslateLoader),
    }),
    provideAppInitializer(() => inject(LanguageService).init()),
  ],
};
