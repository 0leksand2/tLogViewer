import { Injectable, signal } from '@angular/core';

export interface FlightArmChangeMarker {
  /** Unix millisecond when armed state changed. */
  changedAtMs: number;
  /** true = armed, false = disarmed. */
  armed: boolean;
}

/** Singleton holding arm/disarm change timecodes for the active flight. */
@Injectable({ providedIn: 'root' })
export class FlightArmChangeService {
  /** Arm-change markers for the loaded flight (empty when idle). */
  readonly markers = signal<readonly FlightArmChangeMarker[]>([]);

  /** Unix-ms timecodes only. */
  readonly timecodes = signal<readonly number[]>([]);

  setMarkers(markers: readonly FlightArmChangeMarker[]): void {
    const sorted = [...markers].sort((a, b) => a.changedAtMs - b.changedAtMs);
    this.markers.set(sorted);
    this.timecodes.set(sorted.map((marker) => marker.changedAtMs));
  }

  clear(): void {
    this.markers.set([]);
    this.timecodes.set([]);
  }
}
