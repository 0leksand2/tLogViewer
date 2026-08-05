import { Component, inject, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FlightSummaryReport } from '../../tlog-load-menu/models/mav-message.models';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';
import { LanguageService } from '../../core/i18n/language.service';

export type HdopHealthTone = 'healthy' | 'warn' | 'bad' | 'unknown';

@Component({
  selector: 'app-flight-summary-report',
  standalone: true,
  imports: [DecimalPipe, TranslatePipe],
  hostDirectives: [ModalContentHostDirective],
  providers: [{ provide: ModalContentBase, useExisting: FlightSummaryReportComponent }],
  templateUrl: './flight-summary-report.html',
  styleUrl: './flight-summary-report.scss',
})
export class FlightSummaryReportComponent extends ModalContentBase<null> {
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);

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
    this.language.lang();
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

    return fallbackUtc?.trim() || this.translate.instant('common.emDash');
  }

  protected formatHdop(value: number | null | undefined): string {
    this.language.lang();
    if (value == null || !Number.isFinite(value)) {
      return this.translate.instant('common.emDash');
    }
    return value.toFixed(2);
  }

  protected formatCoord(value: number): string {
    return value.toFixed(7);
  }

  protected formatDistanceKm(meters: number): string {
    this.language.lang();
    if (!Number.isFinite(meters)) {
      return this.translate.instant('common.emDash');
    }
    return this.translate.instant('summary.distanceKm', {
      value: (meters / 1000).toFixed(2),
    });
  }

  protected hdopHealthLabel(health: string | null | undefined): string {
    this.language.lang();
    switch (health) {
      case 'Healthy':
        return this.translate.instant('summary.hdopHealthy');
      case 'PossiblyUnhealthy':
        return this.translate.instant('summary.hdopPossiblyUnhealthy');
      case 'Unhealthy':
        return this.translate.instant('summary.hdopUnhealthy');
      default:
        return this.translate.instant('summary.hdopUnknown');
    }
  }

  protected yawCogHealthLabel(health: string | null | undefined): string {
    this.language.lang();
    switch (health) {
      case 'Good':
        return this.translate.instant('summary.yawCogGood');
      case 'Ok':
        return this.translate.instant('summary.yawCogOk');
      case 'Bad':
        return this.translate.instant('summary.yawCogBad');
      default:
        return this.translate.instant('summary.yawCogUnknown');
    }
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
