import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'tlog-viewer.map-display-settings';

export interface MapDisplaySettings {
  /** Red yaw / aircraft heading line. */
  displayHeading: boolean;
  /** Orange nav-bearing (target direction) line. */
  displayTargetPath: boolean;
  /** Dark green wind-direction line. */
  displayWind: boolean;
}

const DEFAULTS: MapDisplaySettings = {
  displayHeading: true,
  displayTargetPath: true,
  displayWind: true,
};

@Injectable({ providedIn: 'root' })
export class MapDisplaySettingsService {
  readonly displayHeading = signal(DEFAULTS.displayHeading);
  readonly displayTargetPath = signal(DEFAULTS.displayTargetPath);
  readonly displayWind = signal(DEFAULTS.displayWind);

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
    } catch {
      // keep defaults
    }
  }

  private persist(): void {
    const payload: MapDisplaySettings = {
      displayHeading: this.displayHeading(),
      displayTargetPath: this.displayTargetPath(),
      displayWind: this.displayWind(),
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
  }
}
