import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RestService } from '../../core/services/rest.service';
import { TlogFlightResult, TlogUploadResult } from '../models/mav-message.models';

export type { TlogUploadResult } from '../models/mav-message.models';

@Injectable({ providedIn: 'root' })
export class TlogService {
  private readonly rest = inject(RestService);

  upload(file: File): Observable<TlogUploadResult> {
    return this.rest.upload<TlogUploadResult>('tlog/upload', file);
  }

  getFlight(sessionId: string, flightId: string): Observable<TlogFlightResult> {
    return this.rest.get<TlogFlightResult>(`tlog/sessions/${sessionId}/flights/${flightId}`);
  }
}
