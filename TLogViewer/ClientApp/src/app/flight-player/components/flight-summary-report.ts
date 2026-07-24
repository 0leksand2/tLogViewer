import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FlightSummaryReport } from '../../tlog-load-menu/models/mav-message.models';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';

export type HdopHealthTone = 'healthy' | 'warn' | 'bad' | 'unknown';

@Component({
  selector: 'app-flight-summary-report',
  standalone: true,
  imports: [DecimalPipe],
  hostDirectives: [ModalContentHostDirective],
  providers: [{ provide: ModalContentBase, useExisting: FlightSummaryReportComponent }],
  templateUrl: './flight-summary-report.html',
  styleUrl: './flight-summary-report.scss',
})
export class FlightSummaryReportComponent extends ModalContentBase<null> {
  readonly report = input<FlightSummaryReport | null>(null);
  /** Emits the event timestamp (Unix ms); parent seeks to 5s before it. */
  readonly seekToMs = output<number>();

  override getModalValue(): null {
    return null;
  }

  protected onEventClick(timestampMs: number): void {
    if (!Number.isFinite(timestampMs)) {
      return;
    }
    this.seekToMs.emit(timestampMs);
  }

  protected formatEventTime(timestampMs: number, fallbackUtc?: string | null): string {
    if (Number.isFinite(timestampMs)) {
      const date = new Date(timestampMs);
      if (!Number.isNaN(date.getTime())) {
        const pad = (n: number) => String(n).padStart(2, '0');
        return (
          `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}` +
          ` ${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}:${pad(date.getUTCSeconds())}` +
          `.${String(date.getUTCMilliseconds()).padStart(3, '0')} UTC`
        );
      }
    }

    return fallbackUtc?.trim() || '—';
  }

  protected formatHdop(value: number | null | undefined): string {
    if (value == null || !Number.isFinite(value)) {
      return '—';
    }
    return value.toFixed(2);
  }

  protected formatCoord(value: number): string {
    return value.toFixed(7);
  }

  protected formatDistanceKm(meters: number): string {
    if (!Number.isFinite(meters)) {
      return '—';
    }
    return `${(meters / 1000).toFixed(2)} km`;
  }

  protected hdopTone(health: string | null | undefined): HdopHealthTone {
    switch (health) {
      case 'Healthy':
        return 'healthy';
      case 'PossiblyUnhealthy':
        return 'warn';
      case 'Unhealthy':
        return 'bad';
      default:
        return 'unknown';
    }
  }

  protected yawCogTone(health: string | null | undefined): HdopHealthTone {
    switch (health) {
      case 'Good':
        return 'healthy';
      case 'Ok':
        return 'warn';
      case 'Bad':
        return 'bad';
      default:
        return 'unknown';
    }
  }

  protected hdopToneForValue(value: number | null | undefined): HdopHealthTone {
    if (value == null || !Number.isFinite(value)) {
      return 'unknown';
    }
    if (value < 0.1 || value >= 1.5) {
      return 'bad';
    }
    if (value < 0.35 || value >= 0.75) {
      return 'warn';
    }
    return 'healthy';
  }
}
