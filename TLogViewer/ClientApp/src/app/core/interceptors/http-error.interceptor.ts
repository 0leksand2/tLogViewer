import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { catchError, throwError } from 'rxjs';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      toastr.error(resolveErrorMessage(error), resolveErrorTitle(error), {
        timeOut: 6000,
        closeButton: true,
      });
      return throwError(() => error);
    }),
  );
};

function resolveErrorTitle(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Network error';
  }
  return `Error ${error.status}`;
}

function resolveErrorMessage(error: HttpErrorResponse): string {
  const body = error.error as
    | { message?: string; title?: string; error?: string }
    | string
    | null
    | undefined;

  if (typeof body === 'string' && body.trim()) {
    // Browser/proxy "Failed to fetch" often means the API process is down or unreachable.
    if (/failed to fetch/i.test(body) || /networkerror/i.test(body)) {
      return 'Unable to reach the API. Ensure TLogViewer.Web is running on http://localhost:5117.';
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
    return 'Unable to reach the API. Ensure TLogViewer.Web is running on http://localhost:5117.';
  }

  if (error.status === 400) {
    return 'The request was invalid.';
  }

  if (error.status === 401) {
    return 'You are not authorized.';
  }

  if (error.status === 403) {
    return 'Access denied.';
  }

  if (error.status === 404) {
    return 'The requested resource was not found.';
  }

  if (error.status >= 500) {
    return 'The server encountered an error.';
  }

  return error.message || 'An unexpected error occurred.';
}
