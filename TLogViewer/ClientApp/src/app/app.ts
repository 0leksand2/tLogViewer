import { Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, switchMap, of, catchError, EMPTY } from 'rxjs';
import { MapComponent } from './map/components/map';
import { MapModule } from './map/map.module';
import { MapDisplayHelpComponent } from './map/components/map-display-help';
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
import { FlightSummaryReportComponent } from './flight-player/components/flight-summary-report';
import {
  resolveActiveHomePoint,
  resolveFlightHomePoints,
  resolveFlightModeChangePoints,
  resolveFlightArmChangePoints,
  resolvePlanePosition,
  resolvePlaybackPoint,
  resolvePositionTarget,
  sortedPlaybackPoints,
  ensureDerivedPlaybackValues,
  progressPercentForMs,
} from './flight-player/utils/playback-timeline';
import {
  MapDisplaySettings,
  MapDisplaySettingsService,
} from './map/services/map-display-settings.service';
import { buildFlightTrail, TRAIL_SAMPLE_MS } from './map/utils/flight-trail';
import { CurrentValue } from './core/services/current.value';
import { FlightModeChangeService } from './core/services/flight-mode-change.service';
import { FlightArmChangeService } from './core/services/flight-arm-change.service';
import { LanguageService } from './core/i18n/language.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MapModule,
    MapDisplayHelpComponent,
    SideMenuModule,
    TlogLoadMenuModule,
    DropdownModule,
    MissionPlannerPropertiesModule,
    ModalModule,
    FlightPlayerModule,
    FlightSummaryReportComponent,
    TranslatePipe,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly selectedPropertiesStorage = inject(SelectedTelemetryPropertiesStorage);
  private readonly tlogService = inject(TlogService);
  private readonly currentValue = inject(CurrentValue);
  private readonly flightModeChanges = inject(FlightModeChangeService);
  private readonly flightArmChanges = inject(FlightArmChangeService);
  private readonly mapDisplaySettings = inject(MapDisplaySettingsService);
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly flightSelection$ = new Subject<{ sessionId: string; flightId: string } | null>();
  private lastTrailBuildKey = '';
  /** Skip redundant UI drives when the resolved playback millisecond is unchanged. */
  private lastDrivenPlaybackMs: number | null = null;
  private lastDrivenFlightId: string | null = null;
  private lastPlaneDriveKey = '';
  private lastHomeDriveKey = '';

  protected readonly menuOpen = signal(true);
  protected readonly propertiesModalOpen = signal(false);
  protected readonly settingsModalOpen = signal(false);
  protected readonly helpModalOpen = signal(false);
  protected readonly summaryModalOpen = signal(false);
  protected readonly displayHeading = this.mapDisplaySettings.displayHeading;
  protected readonly displayTargetPath = this.mapDisplaySettings.displayTargetPath;
  protected readonly displayWind = this.mapDisplaySettings.displayWind;
  protected readonly displayTrail = this.mapDisplaySettings.displayTrail;
  protected readonly displayFullTrail = this.mapDisplaySettings.displayFullTrail;
  protected readonly trailLengthSeconds = this.mapDisplaySettings.trailLengthSeconds;
  protected readonly gpsSource = this.mapDisplaySettings.gpsSource;
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

  protected readonly flightOptions = computed<DropdownOption[]>(() => {
    this.language.lang();
    return this.flightSummaries().map((flight, index) => ({
      value: flight.id,
      label: this.formatFlightOption(flight, index),
    }));
  });

  private readonly map = viewChild(MapComponent);

  constructor() {
    effect(() => {
      this.menuOpen();
      window.setTimeout(() => this.map()?.invalidateSize(), 220);
    });

    effect(() => {
      const flight = this.loadedFlight();
      const points = this.playbackPoints();
      const millisecond = resolvePlaybackPoint(points, this.flightProgressPercent());
      const flightId = flight?.id ?? null;

      // Progress updates every animation frame; only push telemetry when the
      // displayed log millisecond actually changes (keeps 100% pacing realtime).
      if (
        millisecond === this.lastDrivenPlaybackMs &&
        flightId === this.lastDrivenFlightId
      ) {
        return;
      }
      this.lastDrivenPlaybackMs = millisecond;
      this.lastDrivenFlightId = flightId;

      const rawValues =
        millisecond !== null && flight ? (flight.messages[String(millisecond)] ?? {}) : {};
      const values = ensureDerivedPlaybackValues(
        flight?.messages,
        points,
        millisecond,
        rawValues,
      );
      this.currentValue.set(millisecond, values, flightId);
    });

    effect(() => {
      const flight = this.loadedFlight();
      const playbackMs = resolvePlaybackPoint(
        this.playbackPoints(),
        this.flightProgressPercent(),
      );
      const map = this.map();
      const homeKey = `${flight?.id ?? ''}|${playbackMs}`;

      if (!map) {
        return;
      }

      if (homeKey === this.lastHomeDriveKey) {
        return;
      }
      this.lastHomeDriveKey = homeKey;

      const homePoints = resolveFlightHomePoints(flight?.homePoints, flight?.messages);
      const home = resolveActiveHomePoint(homePoints, playbackMs);

      if (!home) {
        map.setHomeLocation(null, null);
        return;
      }

      map.setHomeLocation(
        home.latitudeDeg,
        home.longitudeDeg,
        flight?.id ?? null,
        { recenter: true },
        home.altitudeM,
      );
    });

    effect(() => {
      const flight = this.loadedFlight();
      const playbackMs = resolvePlaybackPoint(
        this.playbackPoints(),
        this.flightProgressPercent(),
      );
      // Track trail settings even when the plane is absent so length/full-trail
      // changes always rebuild the trail on the next pose update.
      const showTrail = this.mapDisplaySettings.displayTrail();
      const fullTrail = this.mapDisplaySettings.displayFullTrail();
      const trailLengthSeconds = this.mapDisplaySettings.trailLengthSeconds();
      const gpsSource = this.mapDisplaySettings.gpsSource();
      const points = this.playbackPoints();
      const map = this.map();

      const planeKey = [
        flight?.id ?? '',
        playbackMs,
        showTrail,
        fullTrail,
        trailLengthSeconds,
        gpsSource,
      ].join('|');

      if (!map) {
        return;
      }

      if (planeKey === this.lastPlaneDriveKey) {
        return;
      }
      this.lastPlaneDriveKey = planeKey;

      const plane = resolvePlanePosition(flight?.messages, playbackMs, gpsSource, points);
      const target = resolvePositionTarget(flight?.messages, playbackMs);
      const homePoints = resolveFlightHomePoints(flight?.homePoints, flight?.messages);
      const home = resolveActiveHomePoint(homePoints, playbackMs);

      if (!plane) {
        map.setPlaneLocation(null, null);
        map.setFlightTrail([]);
        this.lastTrailBuildKey = '';
      } else {
        map.setPlaneLocation(
          plane.lat,
          plane.lon,
          plane.yaw,
          flight?.id ?? null,
          plane.navBearing,
          plane.windDir,
          plane.windSpeed,
          { recenter: !home },
        );

        const modeMarkers = this.flightModeChanges.markers();
        const sampleBucket =
          playbackMs === null ? -1 : Math.floor(playbackMs / TRAIL_SAMPLE_MS);
        const modeEpoch = modeMarkers.reduce(
          (count, marker) =>
            playbackMs !== null && marker.changedAtMs <= playbackMs ? count + 1 : count,
          0,
        );
        const trailKey = showTrail
          ? `${flight?.id ?? ''}|${sampleBucket}|${modeEpoch}|${fullTrail ? 'full' : trailLengthSeconds}|${gpsSource}`
          : 'off';

        if (trailKey !== this.lastTrailBuildKey) {
          this.lastTrailBuildKey = trailKey;
          if (showTrail) {
            map.setFlightTrail(
              buildFlightTrail(
                flight?.messages,
                points,
                playbackMs,
                modeMarkers,
                trailLengthSeconds,
                fullTrail,
                gpsSource,
              ),
            );
          } else {
            map.setFlightTrail([]);
          }
        }
      }

      if (!target) {
        map.setTargetLocation(null, null);
      } else {
        const relativeAlt =
          target.altitudeM != null && home?.altitudeM != null
            ? target.altitudeM - home.altitudeM
            : null;
        map.setTargetLocation(target.lat, target.lon, relativeAlt);
      }
    });

    effect(() => {
      const sessionId = this.sessionId();
      const flightId = this.selectedFlightId();
      this.flightProgressPercent.set(0);
      this.flightPlaying.set(false);
      this.lastDrivenPlaybackMs = null;
      this.lastDrivenFlightId = null;
      this.lastPlaneDriveKey = '';
      this.lastHomeDriveKey = '';

      if (!sessionId || !flightId) {
        this.loadedFlight.set(null);
        this.flightModeChanges.clear();
        this.flightArmChanges.clear();
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
        const flight = result?.flight ?? null;
        this.loadedFlight.set(flight);
        this.flightProgressPercent.set(0);
        this.flightPlaying.set(false);

        if (flight) {
          const homePoints = resolveFlightHomePoints(flight.homePoints, flight.messages);
          const modeChangePoints = resolveFlightModeChangePoints(
            flight.modeChangePoints,
            flight.messages,
          );
          const armChangePoints = resolveFlightArmChangePoints(
            flight.armChangePoints,
            flight.messages,
          );
          this.flightModeChanges.setMarkers(modeChangePoints);
          this.flightArmChanges.setMarkers(armChangePoints);
          console.log('Flight home points', {
            flightId: flight.id,
            fromApi: flight.homePoints,
            resolved: homePoints,
          });
          console.log('Flight mode change points', {
            flightId: flight.id,
            fromApi: flight.modeChangePoints,
            resolved: modeChangePoints,
          });
          console.log('Flight arm change points', {
            flightId: flight.id,
            fromApi: flight.armChangePoints,
            resolved: armChangePoints,
          });
        } else {
          this.flightModeChanges.clear();
          this.flightArmChanges.clear();
        }
      });
  }

  protected openPropertiesModal(): void {
    this.propertiesModalOpen.set(true);
  }

  protected openSettingsModal(): void {
    this.settingsModalOpen.set(true);
  }

  protected openHelpModal(): void {
    this.helpModalOpen.set(true);
  }

  protected openSummaryModal(): void {
    this.summaryModalOpen.set(true);
  }

  protected onHelpClosed(): void {
    this.helpModalOpen.set(false);
  }

  protected onSummaryClosed(): void {
    this.summaryModalOpen.set(false);
  }

  /** Jump playback to 5 seconds before the selected summary event. */
  protected onSummarySeek(eventMs: number): void {
    const points = this.playbackPoints();
    if (points.length === 0 || !Number.isFinite(eventMs)) {
      return;
    }

    this.flightPlaying.set(false);
    this.flightProgressPercent.set(progressPercentForMs(points, eventMs - 5_000));
    this.summaryModalOpen.set(false);
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

  protected onSettingsSaved(result: ModalCloseResult): void {
    const value = result.value as MapDisplaySettings | undefined;
    if (!value) {
      return;
    }
    this.lastTrailBuildKey = '';
    this.mapDisplaySettings.applyAll(value);
  }

  protected onSettingsCancelled(): void {
    // Draft is discarded when the modal content is recreated on next open.
  }

  protected onTlogUploaded(result: TlogUploadResult): void {
    this.sessionId.set(result.sessionId);
    this.flightSummaries.set(result.flights);
    this.loadedFlight.set(null);
    this.flightModeChanges.clear();
    this.flightArmChanges.clear();
    this.selectedFlightId.set(result.flights[0]?.id ?? null);
  }

  private persistSelectedProperties(properties: MissionPlannerProperty[]): void {
    this.selectedProperties.set(properties);
    this.selectedPropertiesStorage.save(properties);
  }

  private formatFlightOption(flight: FlightSummary, index: number): string {
    const duration = this.formatDuration(flight.durationSeconds);
    return this.translate.instant('app.flightOption', {
      index: index + 1,
      duration,
      count: flight.messageCount,
    });
  }

  private formatDuration(seconds: number): string {
    const total = Math.max(0, Math.round(seconds));
    const minutes = Math.floor(total / 60);
    const remainder = total % 60;
    return minutes > 0 ? `${minutes}m ${remainder}s` : `${remainder}s`;
  }
}
