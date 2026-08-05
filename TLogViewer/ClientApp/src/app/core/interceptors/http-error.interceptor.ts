import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);
  const translate = inject(TranslateService);

  // Translation JSON loads must not toast (and may run before strings exist).
  if (req.url.includes('/assets/i18n/')) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      toastr.error(resolveErrorMessage(error, translate), resolveErrorTitle(error, translate), {
        timeOut: 6000,
        closeButton: true,
      });
      return throwError(() => error);
    }),
  );
};

function resolveErrorTitle(error: HttpErrorResponse, translate: TranslateService): string {
  if (error.status === 0) {
    return translate.instant('errors.network');
  }
  return translate.instant('errors.errorStatus', { status: error.status });
}

function resolveErrorMessage(error: HttpErrorResponse, translate: TranslateService): string {
  const body = error.error as
    | { message?: string; title?: string; error?: string }
    | string
    | null
    | undefined;

  if (typeof body === 'string' && body.trim()) {
    if (/failed to fetch/i.test(body) || /networkerror/i.test(body)) {
      return translate.instant('errors.apiUnreachable');
    }
    return body;
  }

  if (body && typeof body === 'object') {
    if (typeof body.error === 'string' && body.error.trim()) {
      return body.error;
    }
    if (typeof body.message === 'string' && body.message.trim()) {
      return body.message;
    }
    if (typeof body.title === 'string' && body.title.trim()) {
      return body.title;
    }
  }

  if (error.status === 0) {
    return translate.instant('errors.apiUnreachable');
  }

  if (error.status === 400) {
    return translate.instant('errors.invalidRequest');
  }

  if (error.status === 401) {
    return translate.instant('errors.unauthorized');
  }

  if (error.status === 403) {
    return translate.instant('errors.accessDenied');
  }

  if (error.status === 404) {
    return translate.instant('errors.notFound');
  }

  if (error.status >= 500) {
    return translate.instant('errors.serverError');
  }

  return error.message || translate.instant('errors.unexpected');
}
