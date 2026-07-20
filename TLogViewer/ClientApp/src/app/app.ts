import { Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, of, catchError, EMPTY } from 'rxjs';
import { MapComponent } from './map/components/map';
import { MapModule } from './map/map.module';
import { SideMenuModule } from './side-menu/side-menu.module';
import { DropdownModule } from './shared/dropdown/dropdown.module';
import { ModalModule } from './shared/modal/modal.module';
import { DropdownOption } from './shared/dropdown/models/dropdown-option.model';
import { MissionPlannerPropertiesModule } from './mission-planner-properties/mission-planner-properties.module';
import { TlogLoadMenuModule } from './tlog-load-menu/tlog-load-menu.module';
import { TlogService } from './tlog-load-menu/services/tlog.service';
import {
  Flight,
  FlightSummary,
  TlogFlightResult,
  TlogUploadResult,
} from './tlog-load-menu/models/mav-message.models';
import { ModalCloseResult } from './shared/modal/models/modal-content.model';
import {
  MissionPlannerProperty,
  MissionPlannerPropertyKey,
} from './mission-planner-properties/models/mission-planner-properties.const';
import { SelectedTelemetryPropertiesStorage } from './mission-planner-properties/services/selected-telemetry-properties-storage.service';
import { FlightPlayerModule } from './flight-player/flight-player.module';
import {
  resolvePlaybackPoint,
  sortedPlaybackPoints,
} from './flight-player/utils/playback-timeline';
import { CurrentValue } from './core/services/current.value';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MapModule,
    SideMenuModule,
    TlogLoadMenuModule,
    DropdownModule,
    MissionPlannerPropertiesModule,
    ModalModule,
    FlightPlayerModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly selectedPropertiesStorage = inject(SelectedTelemetryPropertiesStorage);
  private readonly tlogService = inject(TlogService);
  private readonly currentValue = inject(CurrentValue);
  private readonly flightSelection$ = new Subject<{ sessionId: string; flightId: string } | null>();

  protected readonly menuOpen = signal(true);
  protected readonly propertiesModalOpen = signal(false);
  protected readonly selectedProperties = signal<MissionPlannerProperty[]>(
    this.selectedPropertiesStorage.load(),
  );
  protected readonly sessionId = signal<string | null>(null);
  protected readonly flightSummaries = signal<FlightSummary[]>([]);
  protected readonly selectedFlightId = signal<string | null>(null);
  protected readonly loadedFlight = signal<Flight | null>(null);
  protected readonly flightProgressPercent = signal(0);
  protected readonly flightPlaying = signal(false);
  protected readonly flightPlaybackSpeed = signal(100);

  protected readonly selectedPropertyKeys = computed<MissionPlannerPropertyKey[]>(() =>
    [...this.selectedProperties()]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((property) => property.key),
  );

  protected readonly orderedSelectedProperties = computed(() =>
    [...this.selectedProperties()].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
  );

  protected readonly selectedFlightSummary = computed(
    () => this.flightSummaries().find((flight) => flight.id === this.selectedFlightId()) ?? null,
  );

  protected readonly selectedFlightDuration = computed(
    () => this.loadedFlight()?.durationSeconds ?? this.selectedFlightSummary()?.durationSeconds ?? 0,
  );

  /** Each messages-dict key is a point on the playback scale. */
  protected readonly playbackPoints = computed(() =>
    sortedPlaybackPoints(this.loadedFlight()?.messages),
  );

  protected readonly flightOptions = computed<DropdownOption[]>(() =>
    this.flightSummaries().map((flight, index) => ({
      value: flight.id,
      label: this.formatFlightOption(flight, index),
    })),
  );

  private readonly map = viewChild(MapComponent);

  constructor() {
    effect(() => {
      this.menuOpen();
      window.setTimeout(() => this.map()?.invalidateSize(), 220);
    });

    effect(() => {
      const flight = this.loadedFlight();
      const millisecond = resolvePlaybackPoint(
        this.playbackPoints(),
        this.flightProgressPercent(),
      );
      const values =
        millisecond !== null && flight ? (flight.messages[String(millisecond)] ?? {}) : {};
      this.currentValue.set(millisecond, values, flight?.id ?? null);
    });

    effect(() => {
      const sessionId = this.sessionId();
      const flightId = this.selectedFlightId();
      this.flightProgressPercent.set(0);
      this.flightPlaying.set(false);

      if (!sessionId || !flightId) {
        this.loadedFlight.set(null);
        this.flightSelection$.next(null);
        return;
      }

      this.flightSelection$.next({ sessionId, flightId });
    });

    this.flightSelection$
      .pipe(
        switchMap((selection) => {
          if (!selection) {
            return of(null);
          }
          return this.tlogService.getFlight(selection.sessionId, selection.flightId).pipe(
            catchError(() => EMPTY),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result: TlogFlightResult | null) => {
        this.loadedFlight.set(result?.flight ?? null);
        this.flightProgressPercent.set(0);
        this.flightPlaying.set(false);
      });
  }

  protected openPropertiesModal(): void {
    this.propertiesModalOpen.set(true);
  }

  protected onPropertiesSaved(result: ModalCloseResult): void {
    const value = result.value as MissionPlannerProperty[] | undefined;
    if (value) {
      this.persistSelectedProperties(value);
    }
  }

  protected onPropertiesReordered(properties: MissionPlannerProperty[]): void {
    this.persistSelectedProperties(properties);
  }

  protected onPropertiesCancelled(): void {
    // Selection is reset when the modal content is recreated on next open.
  }

  protected onTlogUploaded(result: TlogUploadResult): void {
    this.sessionId.set(result.sessionId);
    this.flightSummaries.set(result.flights);
    this.loadedFlight.set(null);
    this.selectedFlightId.set(result.flights[0]?.id ?? null);
  }

  private persistSelectedProperties(properties: MissionPlannerProperty[]): void {
    this.selectedProperties.set(properties);
    this.selectedPropertiesStorage.save(properties);
  }

  private formatFlightOption(flight: FlightSummary, index: number): string {
    const duration = this.formatDuration(flight.durationSeconds);
    return `Flight ${index + 1} · ${duration} · ${flight.messageCount} msgs`;
  }

  private formatDuration(seconds: number): string {
    const total = Math.max(0, Math.round(seconds));
    const minutes = Math.floor(total / 60);
    const remainder = total % 60;
    return minutes > 0 ? `${minutes}m ${remainder}s` : `${remainder}s`;
  }
}
