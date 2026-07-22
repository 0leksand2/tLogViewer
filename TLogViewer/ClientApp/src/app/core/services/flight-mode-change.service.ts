import { Injectable, signal } from '@angular/core';

export interface FlightModeChangeMarker {
  /** Unix millisecond when customMode changed. */
  changedAtMs: number;
  /** HEARTBEAT customMode value after the change. */
  customMode: number;
}

/** Singleton holding flight-mode change timecodes for the active flight. */
@Injectable({ providedIn: 'root' })
export class FlightModeChangeService {
  /** Mode-change markers for the loaded flight (empty when idle). */
  readonly markers = signal<readonly FlightModeChangeMarker[]>([]);

  /** Unix-ms timecodes only. */
  readonly timecodes = signal<readonly number[]>([]);

  setMarkers(markers: readonly FlightModeChangeMarker[]): void {
    const sorted = [...markers].sort((a, b) => a.changedAtMs - b.changedAtMs);
    this.markers.set(sorted);
    this.timecodes.set(sorted.map((marker) => marker.changedAtMs));
  }

  clear(): void {
    this.markers.set([]);
    this.timecodes.set([]);
  }
}
