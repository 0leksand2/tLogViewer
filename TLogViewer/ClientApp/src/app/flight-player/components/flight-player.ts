import {
  Component,
  DestroyRef,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  model,
} from '@angular/core';
import { DropdownModule } from '../../shared/dropdown/dropdown.module';
import { DropdownOption } from '../../shared/dropdown/models/dropdown-option.model';
import { snapProgressPercent } from '../utils/playback-timeline';

/** Playback rate as percent of base step rate (100 = 0.1% per step). */
const PLAYBACK_SPEEDS = [1, 10, 50, 75, 100, 125, 150, 200, 500, 1000] as const;
const FORWARD_PERCENT = 5;
/** Progress advance per move at 100% playback speed. */
const STEP_PERCENT_AT_100 = 0.1;
const STEPS_AT_100 = 100 / STEP_PERCENT_AT_100;

@Component({
  selector: 'app-flight-player',
  standalone: true,
  imports: [DropdownModule],
  templateUrl: './flight-player.html',
  styleUrl: './flight-player.scss',
})
export class FlightPlayerComponent implements OnDestroy {
  /**
   * Sorted Unix-ms message keys — each is a point on the playback scale.
   */
  readonly playbackPoints = input<number[]>([]);
  /** Optional wall-clock span for pacing; falls back to point time span. */
  readonly durationSeconds = input(0);
  /** Progress through the flight, 0–100 in 0.1% steps. */
  readonly progressPercent = model(0);
  readonly playing = model(false);
  /** Playback rate (100 = one 0.1% move per step interval). */
  readonly playbackSpeed = model<number>(100);

  protected readonly forwardPercent = FORWARD_PERCENT;

  protected readonly speedOptions: DropdownOption[] = PLAYBACK_SPEEDS.map((speed) => ({
    value: String(speed),
    label: `${speed}%`,
  }));

  private readonly destroyRef = inject(DestroyRef);
  private rafId: number | null = null;
  private lastFrameMs: number | null = null;
  private stepAccumulator = 0;

  protected readonly hasPlayback = computed(() => this.playbackPoints().length > 0);

  protected readonly progressLabel = computed(() => this.formatPercent(this.progressPercent()));
  protected readonly endLabel = computed(() => this.formatPercent(100));

  protected readonly speedValue = computed(() => String(this.playbackSpeed()));

  /** Seconds between 0.1% moves when speed is 100%. */
  private readonly stepIntervalSeconds = computed(() => {
    const points = this.playbackPoints();
    const durationInput = this.durationSeconds();
    if (durationInput > 0) {
      return durationInput / STEPS_AT_100;
    }
    if (points.length < 2) {
      return 0.01;
    }
    const spanSeconds = (points[points.length - 1]! - points[0]!) / 1000;
    return Math.max(spanSeconds, 0.001) / STEPS_AT_100;
  });

  constructor() {
    effect(() => {
      if (!this.hasPlayback()) {
        this.stopPlayback();
        this.playing.set(false);
        this.progressPercent.set(0);
        return;
      }

      const snapped = snapProgressPercent(this.progressPercent());
      if (snapped !== this.progressPercent()) {
        this.progressPercent.set(snapped);
      }
    });

    effect(() => {
      if (this.playing() && this.hasPlayback()) {
        this.startPlayback();
      } else {
        this.stopPlayback();
      }
    });

    this.destroyRef.onDestroy(() => this.stopPlayback());
  }

  ngOnDestroy(): void {
    this.stopPlayback();
  }

  protected togglePlay(): void {
    if (!this.hasPlayback()) {
      return;
    }

    if (this.progressPercent() >= 100) {
      this.progressPercent.set(0);
    }

    this.playing.update((value) => !value);
  }

  protected forward(): void {
    if (!this.hasPlayback()) {
      return;
    }

    const next = snapProgressPercent(this.progressPercent() + FORWARD_PERCENT);
    this.progressPercent.set(next);
    if (next >= 100) {
      this.playing.set(false);
    }
  }

  protected onSliderInput(event: Event): void {
    if (!this.hasPlayback()) {
      return;
    }

    const input = event.target as HTMLInputElement;
    const value = Number(input.value);
    if (!Number.isFinite(value)) {
      return;
    }

    this.progressPercent.set(snapProgressPercent(value));
  }

  protected onSpeedChange(value: string | null): void {
    if (value === null) {
      return;
    }
    const parsed = Number(value);
    if (!Number.isFinite(parsed)) {
      return;
    }
    this.playbackSpeed.set(parsed);
  }

  private startPlayback(): void {
    if (this.rafId !== null) {
      return;
    }

    this.lastFrameMs = null;
    this.stepAccumulator = 0;

    const tick = (now: number) => {
      if (!this.playing()) {
        this.rafId = null;
        this.lastFrameMs = null;
        return;
      }

      if (this.lastFrameMs === null) {
        this.lastFrameMs = now;
      }

      const deltaSeconds = (now - this.lastFrameMs) / 1000;
      this.lastFrameMs = now;

      // At 100% speed: one 0.1% move every stepIntervalSeconds.
      const speedFactor = this.playbackSpeed() / 100;
      this.stepAccumulator += deltaSeconds * speedFactor;

      const interval = this.stepIntervalSeconds();
      if (interval <= 0) {
        this.playing.set(false);
        this.rafId = null;
        this.lastFrameMs = null;
        return;
      }

      let progress = this.progressPercent();
      while (this.stepAccumulator >= interval) {
        this.stepAccumulator -= interval;
        progress = snapProgressPercent(progress + STEP_PERCENT_AT_100);
        if (progress >= 100) {
          this.progressPercent.set(100);
          this.playing.set(false);
          this.rafId = null;
          this.lastFrameMs = null;
          this.stepAccumulator = 0;
          return;
        }
      }

      if (progress !== this.progressPercent()) {
        this.progressPercent.set(progress);
      }

      this.rafId = requestAnimationFrame(tick);
    };

    this.rafId = requestAnimationFrame(tick);
  }

  private stopPlayback(): void {
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    this.lastFrameMs = null;
    this.stepAccumulator = 0;
  }

  private formatPercent(value: number): string {
    const rounded = Math.round(value * 10) / 10;
    return `${rounded}%`;
  }
}
