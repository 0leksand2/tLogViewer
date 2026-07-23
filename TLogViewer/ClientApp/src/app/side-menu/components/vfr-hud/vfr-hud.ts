import { Component, computed, inject } from '@angular/core';
import { CurrentValue } from '../../../core/services/current.value';
import { FlightFieldIds } from '../../../core/flight-field-ids';

/** viewBox units of horizon shift per degree of pitch. */
const PITCH_PX_PER_DEG = 1.35;
/** Visible pitch travel clamp so sky/ground always fill the dial. */
const PITCH_CLAMP_DEG = 80;

/** Pitch ladder marks (degrees) drawn on the attitude ball. */
const PITCH_TICKS = [-60, -40, -20, -10, 10, 20, 40, 60] as const;

@Component({
  selector: 'app-vfr-hud',
  standalone: true,
  templateUrl: './vfr-hud.html',
  styleUrl: './vfr-hud.scss',
})
export class VfrHudComponent {
  private readonly currentValue = inject(CurrentValue);

  protected readonly pitchTicks = PITCH_TICKS;
  protected readonly pitchPxPerDeg = PITCH_PX_PER_DEG;

  protected readonly rollDeg = computed(() =>
    readDegrees(this.currentValue.values()[FlightFieldIds.AttitudeRollDeg]),
  );

  protected readonly pitchDeg = computed(() =>
    readDegrees(this.currentValue.values()[FlightFieldIds.AttitudePitchDeg]),
  );

  protected readonly rollLabel = computed(() => formatAttitude(this.rollDeg()));
  protected readonly pitchLabel = computed(() => formatAttitude(this.pitchDeg()));

  protected readonly ariaLabel = computed(
    () => `VFR HUD, roll ${this.rollLabel()}, pitch ${this.pitchLabel()}`,
  );

  /** Horizon ball: roll rotation then pitch translation (nose-up → horizon down). */
  protected readonly horizonTransform = computed(() => {
    const roll = this.rollDeg();
    const pitch = Math.max(-PITCH_CLAMP_DEG, Math.min(PITCH_CLAMP_DEG, this.pitchDeg()));
    const pitchY = pitch * PITCH_PX_PER_DEG;
    return `rotate(${-roll} 100 100) translate(0 ${pitchY})`;
  });
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

function formatAttitude(degrees: number): string {
  const rounded = Math.round(degrees * 10) / 10;
  const sign = rounded > 0 ? '+' : '';
  return `${sign}${rounded.toFixed(1)}°`;
}
