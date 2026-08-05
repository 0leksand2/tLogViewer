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
import { FlightModeChangeService } from '../../core/services/flight-mode-change.service';
import { FlightArmChangeService } from '../../core/services/flight-arm-change.service';
import { flightModeLabel } from '../../core/flight-mode';
import { LanguageService } from '../../core/i18n/language.service';
import { snapProgressPercent } from '../utils/playback-timeline';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

/** Playback rate as percent of realtime (100 = 1 ms wall-clock → 1 ms of log). */
const PLAYBACK_SPEEDS = [1, 10, 50, 75, 100, 125, 150, 200, 500, 1000] as const;
const FORWARD_PERCENT = 5;
/** Seek this far before a mode-change marker when the marker is clicked. */
const MODE_MARKER_SEEK_BEFORE_MS = 5_000;

@Component({
  selector: 'app-flight-player',
  standalone: true,
  imports: [DropdownModule, TranslatePipe],
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
  private readonly flightModeChanges = inject(FlightModeChangeService);
  private readonly flightArmChanges = inject(FlightArmChangeService);
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private rafId: number | null = null;
  /**
   * Absolute realtime anchor: wall clock `anchorWallMs` corresponds to log time
   * `anchorLogMs`. Avoids drift from integrating per-frame deltas under load.
   */
  private anchorWallMs: number | null = null;
  private anchorLogMs: number | null = null;

  protected readonly hasPlayback = computed(() => this.playbackPoints().length > 0);

  protected readonly progressLabel = computed(() => this.formatPercent(this.progressPercent()));
  protected readonly endLabel = computed(() => this.formatPercent(100));

  protected readonly speedValue = computed(() => String(this.playbackSpeed()));

  /** Mode-change ticks positioned on the slider track (0–100%). */
  protected readonly modeChangeMarkers = computed(() => {
    const points = this.playbackPoints();
    const markers = this.flightModeChanges.markers();
    if (points.length < 2 || markers.length === 0) {
      return [] as {
        percent: number;
        customMode: number;
        changedAtMs: number;
        label: string;
      }[];
    }

    const first = points[0]!;
    const last = points[points.length - 1]!;
    const span = Math.max(1, last - first);

    return markers
      .map((marker) => ({
        percent: Math.min(100, Math.max(0, ((marker.changedAtMs - first) / span) * 100)),
        customMode: marker.customMode,
        changedAtMs: marker.changedAtMs,
        label: flightModeLabel(marker.customMode),
      }))
      .filter((marker) => Number.isFinite(marker.percent));
  });

  /** Arm/disarm ticks positioned on the slider track (0–100%). */
  protected readonly armChangeMarkers = computed(() => {
    this.language.lang();
    const points = this.playbackPoints();
    const markers = this.flightArmChanges.markers();
    if (points.length < 2 || markers.length === 0) {
      return [] as {
        percent: number;
        armed: boolean;
        changedAtMs: number;
        label: string;
      }[];
    }

    const first = points[0]!;
    const last = points[points.length - 1]!;
    const span = Math.max(1, last - first);

    return markers
      .map((marker) => ({
        percent: Math.min(100, Math.max(0, ((marker.changedAtMs - first) / span) * 100)),
        armed: marker.armed,
        changedAtMs: marker.changedAtMs,
        label: this.translate.instant(marker.armed ? 'player.arm' : 'player.disarm'),
      }))
      .filter((marker) => Number.isFinite(marker.percent));
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
    this.invalidatePlaybackAnchor();
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
    this.invalidatePlaybackAnchor();
  }

  /** Jump to 5 seconds before the mode-change timecode (clamped to flight start). */
  protected seekToModeMarker(changedAtMs: number): void {
    this.seekBeforeMs(changedAtMs, MODE_MARKER_SEEK_BEFORE_MS);
  }

  /** Jump to the arm/disarm timecode. */
  protected seekToArmMarker(changedAtMs: number): void {
    this.seekBeforeMs(changedAtMs, 0);
  }

  private seekBeforeMs(changedAtMs: number, beforeMs: number): void {
    const points = this.playbackPoints();
    if (points.length < 2) {
      return;
    }

    const first = points[0]!;
    const last = points[points.length - 1]!;
    const span = Math.max(1, last - first);
    const targetMs = Math.max(first, changedAtMs - beforeMs);
    const percent = ((targetMs - first) / span) * 100;
    this.progressPercent.set(snapProgressPercent(percent));
    this.invalidatePlaybackAnchor();
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
    this.invalidatePlaybackAnchor();
  }

  private startPlayback(): void {
    if (this.rafId !== null) {
      return;
    }

    this.invalidatePlaybackAnchor();

    const tick = (now: number) => {
      if (!this.playing()) {
        this.rafId = null;
        this.invalidatePlaybackAnchor();
        return;
      }

      const points = this.playbackPoints();
      if (points.length === 0) {
        this.playing.set(false);
        this.rafId = null;
        this.invalidatePlaybackAnchor();
        return;
      }

      const first = points[0]!;
      const last = points.length >= 2 ? points[points.length - 1]! : first;
      const span = Math.max(1, last - first);

      if (this.anchorWallMs === null || this.anchorLogMs === null) {
        this.anchorWallMs = now;
        this.anchorLogMs = first + (span * this.progressPercent()) / 100;
      }

      // 100% ⇒ 1 ms wall-clock = 1 ms of log time (absolute clock, not summed deltas).
      const speedFactor = this.playbackSpeed() / 100;
      const logMs = this.anchorLogMs + (now - this.anchorWallMs) * speedFactor;

      if (logMs >= last) {
        this.progressPercent.set(100);
        this.playing.set(false);
        this.rafId = null;
        this.invalidatePlaybackAnchor();
        return;
      }

      const percent = ((Math.max(first, logMs) - first) / span) * 100;
      this.progressPercent.set(percent);
      this.rafId = requestAnimationFrame(tick);
    };

    this.rafId = requestAnimationFrame(tick);
  }

  private stopPlayback(): void {
    if (this.rafId !== null) {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
    this.invalidatePlaybackAnchor();
  }

  /** Force the next tick to re-sync wall clock to the current log position. */
  private invalidatePlaybackAnchor(): void {
    this.anchorWallMs = null;
    this.anchorLogMs = null;
  }

  private formatPercent(value: number): string {
    const rounded = Math.round(value * 10) / 10;
    return `${rounded}%`;
  }
}
