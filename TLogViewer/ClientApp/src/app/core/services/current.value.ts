import { Injectable, signal } from '@angular/core';

/** Singleton holding the currently played flight timestamp and field values. */
@Injectable({ providedIn: 'root' })
export class CurrentValue {
  /** Unix millisecond of the active playback point, or `null` when idle. */
  readonly millisecond = signal<number | null>(null);

  /**
   * Merged field values across playback: `{propertyValue → value}`.
   * New milliseconds overwrite matching keys; other keys are kept.
   */
  readonly values = signal<Readonly<Record<string, unknown>>>({});

  private flightId: string | null = null;

  /**
   * Advances playback to `millisecond` and merges `values` into the list.
   * Pass `flightId` when the active flight changes so prior values are cleared.
   */
  set(
    millisecond: number | null,
    values: Readonly<Record<string, unknown>> = {},
    flightId?: string | null,
  ): void {
    if (flightId !== undefined && flightId !== this.flightId) {
      this.flightId = flightId ?? null;
      this.values.set({});
    }

    this.millisecond.set(millisecond);

    if (millisecond === null) {
      this.values.set({});
      return;
    }

    if (Object.keys(values).length === 0) {
      return;
    }

    this.values.update((current) => ({ ...current, ...values }));
  }

  /** Lookup by Mission Planner `propertyValue` key. */
  get(propertyValue: string): unknown {
    return this.values()[propertyValue];
  }
}
