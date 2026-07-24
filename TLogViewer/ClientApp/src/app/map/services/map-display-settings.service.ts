import { Injectable, signal } from '@angular/core';
import {
  DEFAULT_GPS_SOURCE,
  GPS_SOURCE_OPTIONS,
  type MapGpsSource,
  isMapGpsSource,
} from '../models/map-gps-source';

const STORAGE_KEY = 'tlog-viewer.map-display-settings';

export interface MapDisplaySettings {
  /** Red yaw / aircraft heading line. */
  displayHeading: boolean;
  /** Orange nav-bearing (target direction) line. */
  displayTargetPath: boolean;
  /** Dark green wind-direction line. */
  displayWind: boolean;
  /** Mode-colored trail behind the aircraft. */
  displayTrail: boolean;
  /** Trail from flight start to current playback (can be slow). */
  displayFullTrail: boolean;
  /** Trail length in seconds of log time (ignored when full trail is on). */
  trailLengthSeconds: number;
  /** MAVLink message used for aircraft lat/lon on the map. */
  gpsSource: MapGpsSource;
}

export const TRAIL_LENGTH_OPTIONS = [
  { seconds: 10, label: '10s' },
  { seconds: 30, label: '30s' },
  { seconds: 60, label: '1min' },
  { seconds: 120, label: '2min' },
  { seconds: 300, label: '5min' },
  { seconds: 600, label: '10min' },
  { seconds: 1200, label: '20min' },
] as const;

export { GPS_SOURCE_OPTIONS };

const DEFAULTS: MapDisplaySettings = {
  displayHeading: true,
  displayTargetPath: true,
  displayWind: true,
  displayTrail: true,
  displayFullTrail: false,
  trailLengthSeconds: 60,
  gpsSource: DEFAULT_GPS_SOURCE,
};

function clampTrailLength(value: number): number {
  if (!Number.isFinite(value)) {
    return DEFAULTS.trailLengthSeconds;
  }
  return Math.min(1200, Math.max(5, Math.round(value)));
}

function snapTrailLength(value: number): number {
  const clamped = clampTrailLength(value);
  let best: number = TRAIL_LENGTH_OPTIONS[0].seconds;
  for (const option of TRAIL_LENGTH_OPTIONS) {
    if (Math.abs(option.seconds - clamped) < Math.abs(best - clamped)) {
      best = option.seconds;
    }
  }
  return best;
}

function normalizeGpsSource(value: unknown): MapGpsSource {
  return isMapGpsSource(value) ? value : DEFAULT_GPS_SOURCE;
}

@Injectable({ providedIn: 'root' })
export class MapDisplaySettingsService {
  readonly displayHeading = signal(DEFAULTS.displayHeading);
  readonly displayTargetPath = signal(DEFAULTS.displayTargetPath);
  readonly displayWind = signal(DEFAULTS.displayWind);
  readonly displayTrail = signal(DEFAULTS.displayTrail);
  readonly displayFullTrail = signal(DEFAULTS.displayFullTrail);
  readonly trailLengthSeconds = signal(DEFAULTS.trailLengthSeconds);
  readonly gpsSource = signal<MapGpsSource>(DEFAULTS.gpsSource);

  constructor() {
    this.restore();
  }

  setDisplayHeading(value: boolean): void {
    this.displayHeading.set(value);
    this.persist();
  }

  setDisplayTargetPath(value: boolean): void {
    this.displayTargetPath.set(value);
    this.persist();
  }

  setDisplayWind(value: boolean): void {
    this.displayWind.set(value);
    this.persist();
  }

  setDisplayTrail(value: boolean): void {
    this.displayTrail.set(value);
    this.persist();
  }

  setDisplayFullTrail(value: boolean): void {
    this.displayFullTrail.set(value);
    this.persist();
  }

  setTrailLengthSeconds(value: number): void {
    this.trailLengthSeconds.set(snapTrailLength(value));
    this.persist();
  }

  setGpsSource(value: MapGpsSource): void {
    this.gpsSource.set(normalizeGpsSource(value));
    this.persist();
  }

  /** Replace all display settings and persist once. */
  applyAll(settings: MapDisplaySettings): void {
    this.displayHeading.set(!!settings.displayHeading);
    this.displayTargetPath.set(!!settings.displayTargetPath);
    this.displayWind.set(!!settings.displayWind);
    this.displayTrail.set(!!settings.displayTrail);
    this.displayFullTrail.set(!!settings.displayFullTrail);
    this.trailLengthSeconds.set(snapTrailLength(Number(settings.trailLengthSeconds)));
    this.gpsSource.set(normalizeGpsSource(settings.gpsSource));
    this.persist();
  }

  private restore(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return;
      }

      const parsed = JSON.parse(raw) as Partial<MapDisplaySettings>;
      if (typeof parsed.displayHeading === 'boolean') {
        this.displayHeading.set(parsed.displayHeading);
      }
      if (typeof parsed.displayTargetPath === 'boolean') {
        this.displayTargetPath.set(parsed.displayTargetPath);
      }
      if (typeof parsed.displayWind === 'boolean') {
        this.displayWind.set(parsed.displayWind);
      }
      if (typeof parsed.displayTrail === 'boolean') {
        this.displayTrail.set(parsed.displayTrail);
      }
      if (typeof parsed.displayFullTrail === 'boolean') {
        this.displayFullTrail.set(parsed.displayFullTrail);
      }
      if (
        typeof parsed.trailLengthSeconds === 'number' ||
        typeof parsed.trailLengthSeconds === 'string'
      ) {
        this.trailLengthSeconds.set(snapTrailLength(Number(parsed.trailLengthSeconds)));
      }
      if (parsed.gpsSource !== undefined) {
        this.gpsSource.set(normalizeGpsSource(parsed.gpsSource));
      }
    } catch {
      // keep defaults
    }
  }

  private persist(): void {
    const payload: MapDisplaySettings = {
      displayHeading: this.displayHeading(),
      displayTargetPath: this.displayTargetPath(),
      displayWind: this.displayWind(),
      displayTrail: this.displayTrail(),
      displayFullTrail: this.displayFullTrail(),
      trailLengthSeconds: this.trailLengthSeconds(),
      gpsSource: this.gpsSource(),
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
  }
}
