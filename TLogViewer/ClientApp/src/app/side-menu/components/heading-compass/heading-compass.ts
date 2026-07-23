import { Component, computed, inject } from '@angular/core';
import { CurrentValue } from '../../../core/services/current.value';
import { FlightFieldIds } from '../../../core/flight-field-ids';

/** Major compass tick degrees (also used for numeric labels except cardinals). */
const MAJOR_TICKS = [0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330] as const;

@Component({
  selector: 'app-heading-compass',
  standalone: true,
  templateUrl: './heading-compass.html',
  styleUrl: './heading-compass.scss',
})
export class HeadingCompassComponent {
  private readonly currentValue = inject(CurrentValue);

  protected readonly majorTicks = MAJOR_TICKS;
  protected readonly minorTicks = Array.from({ length: 36 }, (_, i) => i * 10).filter(
    (deg) => deg % 30 !== 0,
  );

  protected readonly yawDeg = computed(() => {
    const values = this.currentValue.values();
    return normalizeHeading(
      readDegrees(values[FlightFieldIds.AliasYaw] ?? values[FlightFieldIds.AttitudeYawDeg]),
    );
  });

  protected readonly yawLabel = computed(() => `${this.yawDeg().toFixed(0)}°`);

  protected readonly ariaLabel = computed(
    () => `Heading compass, yaw ${this.yawLabel()}`,
  );

  /**
   * Compass card rotates opposite to yaw so the aircraft (fixed, nose up / north)
   * always points at the current heading on the rose.
   */
  protected readonly compassTransform = computed(
    () => `rotate(${-this.yawDeg()} 100 100)`,
  );

  protected tickLabel(deg: number): string {
    switch (deg) {
      case 0:
        return 'N';
      case 90:
        return 'E';
      case 180:
        return 'S';
      case 270:
        return 'W';
      default:
        return String(deg);
    }
  }

  protected isCardinal(deg: number): boolean {
    return deg === 0 || deg === 90 || deg === 180 || deg === 270;
  }
}

function readDegrees(value: unknown): number {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }
  return 0;
}

function normalizeHeading(degrees: number): number {
  let value = degrees % 360;
  if (value < 0) {
    value += 360;
  }
  return value;
}
