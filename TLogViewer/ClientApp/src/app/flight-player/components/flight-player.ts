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

/** Playback rate as percent of realtime (100 = 1 ms wall-clock → 1 ms of log). */
const PLAYBACK_SPEEDS = [1, 10, 50, 75, 100, 125, 150, 200, 500, 1000] as const;
const FORWARD_PERCENT = 5;

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
  /** Progress through the flight, 0–100. */
  readonly progressPercent = model(0);
  readonly playing = model(false);
  /** Playback rate (100 = realtime along the log timeline). */
  readonly playbackSpeed = model<number>(100);

  protected readonly forwardPercent = FORWARD_PERCENT;

  protected readonly speedOptions: DropdownOption[] = PLAYBACK_SPEEDS.map((speed) => ({
    value: String(speed),
    label: `${speed}%`,
  }));

  private readonly destroyRef = inject(DestroyRef);
  private rafId: number | null = null;
  private lastFrameMs: number | null = null;

  protected readonly hasPlayback = computed(() => this.playbackPoints().length > 0);

  protected readonly progressLabel = computed(() => this.formatPercent(this.progressPercent()));
  protected readonly endLabel = computed(() => this.formatPercent(100));

  protected readonly speedValue = computed(() => String(this.playbackSpeed()));

  /** Log timeline length in milliseconds (first→last message key, or duration). */
  private readonly timelineSpanMs = computed(() => {
    const points = this.playbackPoints();
    if (points.length >= 2) {
      return Math.max(1, points[points.length - 1]! - points[0]!);
    }
    const duration = this.durationSeconds();
    if (duration > 0) {
      return Math.max(1, Math.round(duration * 1000));
    }
    return 1;
  });

  constructor() {
    effect(() => {
      if (!this.hasPlayback()) {
        this.stopPlayback();
        this.playing.set(false);
        this.progressPercent.set(0);
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

  /** Slider scrubbing snaps to 0.1%. */
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

    const tick = (now: number) => {
      if (!this.playing()) {
        this.rafId = null;
        this.lastFrameMs = null;
        return;
      }

      if (this.lastFrameMs === null) {
        this.lastFrameMs = now;
      }

      const deltaWallMs = now - this.lastFrameMs;
      this.lastFrameMs = now;

      // At 100% speed: 1 ms of wall time advances 1 ms of log timeline.
      const speedFactor = this.playbackSpeed() / 100;
      const deltaFlightMs = deltaWallMs * speedFactor;
      const deltaPercent = (deltaFlightMs / this.timelineSpanMs()) * 100;
      const next = this.progressPercent() + deltaPercent;

      if (next >= 100) {
        this.progressPercent.set(100);
        this.playing.set(false);
        this.rafId = null;
        this.lastFrameMs = null;
        return;
      }

      this.progressPercent.set(next);
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
  }

  private formatPercent(value: number): string {
    const rounded = Math.round(value * 10) / 10;
    return `${rounded}%`;
  }
}
