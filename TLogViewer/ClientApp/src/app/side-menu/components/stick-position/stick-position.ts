import { Component, computed, inject } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CurrentValue } from '../../../core/services/current.value';
import { LanguageService } from '../../../core/i18n/language.service';

/** RC_CHANNELS chan1–4 (Mission Planner ch1in–ch4in). */
const CH_ROLL = '65_005';
const CH_PITCH = '65_006';
const CH_THROTTLE = '65_007';
const CH_YAW = '65_008';

/** Typical PWM center / half-range for stick normalization. */
const PWM_CENTER = 1500;
const PWM_HALF_RANGE = 500;

/** Stick travel radius inside the 200×200 viewBox circle. */
const STICK_TRAVEL = 68;

@Component({
  selector: 'app-stick-position',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './stick-position.html',
  styleUrl: './stick-position.scss',
})
export class StickPositionComponent {
  private readonly currentValue = inject(CurrentValue);
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);

  protected readonly travel = STICK_TRAVEL;

  /** Left stick: yaw (X) + throttle (Y, up = high). */
  protected readonly leftDot = computed(() => {
    const values = this.currentValue.values();
    return {
      x: 100 + pwmToNorm(values[CH_YAW]) * STICK_TRAVEL,
      y: 100 - pwmToNorm(values[CH_THROTTLE]) * STICK_TRAVEL,
    };
  });

  /** Right stick: roll (X) + pitch (Y, up = stick back / nose up). */
  protected readonly rightDot = computed(() => {
    const values = this.currentValue.values();
    return {
      x: 100 + pwmToNorm(values[CH_ROLL]) * STICK_TRAVEL,
      y: 100 - pwmToNorm(values[CH_PITCH]) * STICK_TRAVEL,
    };
  });

  protected readonly yawLabel = computed(() => formatPwm(this.currentValue.values()[CH_YAW]));
  protected readonly throttleLabel = computed(() =>
    formatPwm(this.currentValue.values()[CH_THROTTLE]),
  );
  protected readonly rollLabel = computed(() => formatPwm(this.currentValue.values()[CH_ROLL]));
  protected readonly pitchLabel = computed(() => formatPwm(this.currentValue.values()[CH_PITCH]));

  protected readonly ariaLabel = computed(() => {
    this.language.lang();
    return this.translate.instant('gauges.sticksAria', {
      yaw: this.yawLabel(),
      throttle: this.throttleLabel(),
      roll: this.rollLabel(),
      pitch: this.pitchLabel(),
    });
  });
}

/** Maps RC PWM (~1000–2000) to −1…+1 around center 1500. */
function pwmToNorm(value: unknown): number {
  const pwm = readNumber(value);
  if (pwm === null) {
    return 0;
  }
  // Unused MAVLink channels are UINT16_MAX; ignore out-of-range PWM.
  if (pwm < 800 || pwm > 2200) {
    return 0;
  }
  const norm = (pwm - PWM_CENTER) / PWM_HALF_RANGE;
  return Math.max(-1, Math.min(1, norm));
}

function readNumber(value: unknown): number | null {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }
  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }
  return null;
}

function formatPwm(value: unknown): string {
  const pwm = readNumber(value);
  if (pwm === null || pwm < 800 || pwm > 2200) {
    return '—';
  }
  return String(Math.round(pwm));
}
