import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RestRequestOptions {
  headers?: HttpHeaders | Record<string, string | string[]>;
  params?: HttpParams | Record<string, string | number | boolean | ReadonlyArray<string | number | boolean>>;
}

@Injectable({ providedIn: 'root' })
export class RestService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = '/api';

  get<T>(path: string, options?: RestRequestOptions): Observable<T> {
    return this.http.get<T>(this.url(path), options);
  }

  post<T>(path: string, body: unknown, options?: RestRequestOptions): Observable<T> {
    return this.http.post<T>(this.url(path), body, options);
  }

  /** Multipart upload — field name defaults to `file`. */
  upload<T>(
    path: string,
    file: File,
    fieldName = 'file',
    extraFields?: Record<string, string>,
  ): Observable<T> {
    const formData = new FormData();
    formData.append(fieldName, file, file.name);

    if (extraFields) {
      for (const [key, value] of Object.entries(extraFields)) {
        formData.append(key, value);
      }
    }

    return this.post<T>(path, formData);
  }

  private url(path: string): string {
    if (/^https?:\/\//i.test(path)) {
      return path;
    }

    const normalized = path.startsWith('/') ? path : `/${path}`;
    if (normalized.startsWith(`${this.apiBase}/`) || normalized === this.apiBase) {
      return normalized;
    }

    return `${this.apiBase}${normalized}`;
  }
}
