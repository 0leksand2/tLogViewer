import {
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { CurrentValue } from '../../../core/services/current.value';
import { FlightStatusText } from '../../../tlog-load-menu/models/mav-message.models';

export interface StatusTextRow {
  id: string;
  changedAtMs: number;
  timeLabel: string;
  severity: number;
  text: string;
  severityLabel: string;
  line: string;
}

const SEVERITY_LABELS = [
  'EMERGENCY',
  'ALERT',
  'CRITICAL',
  'ERROR',
  'WARNING',
  'NOTICE',
  'INFO',
  'DEBUG',
] as const;

/** Pixels from the bottom still treated as "at bottom". */
const BOTTOM_EPSILON_PX = 8;

@Component({
  selector: 'app-side-menu-messages-tab',
  standalone: true,
  imports: [NgClass],
  templateUrl: './side-menu-messages-tab.html',
  styleUrl: './side-menu-messages-tab.scss',
})
export class SideMenuMessagesTabComponent {
  /** Per-ms STATUSTEXT map from the loaded flight. */
  readonly statusTexts = input<Record<string, FlightStatusText[]> | null>(null);
  /** When true, keep the latest visible message in view unless the user scrolled. */
  readonly playing = input(false);

  private readonly currentValue = inject(CurrentValue);
  private readonly listRef = viewChild<ElementRef<HTMLElement>>('messageList');

  private readonly autoScroll = signal(true);
  protected readonly atBottom = signal(true);
  private programmaticScroll = false;
  private lastScrolledId: string | null = null;
  private wasPlaying = false;

  protected readonly rows = computed((): StatusTextRow[] => {
    const map = this.statusTexts();
    if (!map) {
      return [];
    }

    const rows: StatusTextRow[] = [];
    const keys = Object.keys(map)
      .map((key) => Number(key))
      .filter((ms) => Number.isFinite(ms))
      .sort((a, b) => a - b);

    for (const changedAtMs of keys) {
      const entries = map[String(changedAtMs)] ?? [];
      entries.forEach((entry, index) => {
        const severity = Math.trunc(Number(entry.severity));
        const text = typeof entry.text === 'string' ? entry.text.trim() : '';
        if (!text) {
          return;
        }
        const severityValue = Number.isFinite(severity) ? severity : 6;
        const label = severityLabel(severityValue);
        const timeLabel = formatTimecode(changedAtMs);
        rows.push({
          id: `${changedAtMs}-${index}`,
          changedAtMs,
          timeLabel,
          severity: severityValue,
          text,
          severityLabel: label,
          line: `${timeLabel} - [${label}] - ${text}`,
        });
      });
    }

    return rows;
  });

  /** Messages that have arrived at or before the current playback millisecond. */
  protected readonly visibleRows = computed(() => {
    const rows = this.rows();
    const playbackMs = this.currentValue.millisecond();
    if (playbackMs === null) {
      return rows;
    }
    return rows.filter((row) => row.changedAtMs <= playbackMs);
  });

  protected readonly activeRowId = computed(() => {
    const visible = this.visibleRows();
    return visible.length > 0 ? visible[visible.length - 1]!.id : null;
  });

  protected readonly showScrollToBottom = computed(
    () => this.visibleRows().length > 0 && !this.atBottom(),
  );

  constructor() {
    effect(() => {
      const playing = this.playing();
      if (playing && !this.wasPlaying) {
        this.autoScroll.set(true);
        this.lastScrolledId = null;
      }
      this.wasPlaying = playing;
    });

    effect(() => {
      // Reset follow mode when a new flight / status set loads.
      this.statusTexts();
      this.autoScroll.set(true);
      this.atBottom.set(true);
      this.lastScrolledId = null;
    });

    effect(() => {
      const list = this.listRef();
      const activeId = this.activeRowId();
      const shouldFollow = this.autoScroll();
      if (!list || !activeId || !shouldFollow || activeId === this.lastScrolledId) {
        return;
      }

      queueMicrotask(() => this.scrollToActive(activeId));
    });
  }

  protected onListScroll(): void {
    if (this.programmaticScroll) {
      return;
    }

    const list = this.listRef()?.nativeElement;
    if (!list) {
      return;
    }

    const bottom = isScrolledToBottom(list);
    this.atBottom.set(bottom);
    if (!bottom) {
      this.autoScroll.set(false);
    }
  }

  protected scrollToBottomAndFollow(): void {
    this.autoScroll.set(true);
    this.lastScrolledId = null;
    this.scrollListToBottom();

    const activeId = this.activeRowId();
    if (activeId) {
      queueMicrotask(() => this.scrollToActive(activeId));
    }
  }

  protected severityModifier(severity: number): string {
    if (severity <= 2) {
      return 'critical';
    }
    if (severity <= 3) {
      return 'error';
    }
    if (severity === 4) {
      return 'warning';
    }
    if (severity >= 7) {
      return 'debug';
    }
    return 'info';
  }

  private scrollToActive(activeId: string): void {
    const list = this.listRef()?.nativeElement;
    if (!list) {
      return;
    }

    const target = list.querySelector<HTMLElement>(
      `[data-message-id="${CSS.escape(activeId)}"]`,
    );
    if (!target) {
      this.scrollListToBottom();
      this.lastScrolledId = activeId;
      return;
    }

    this.withProgrammaticScroll(list, () => {
      target.scrollIntoView({ block: 'nearest', behavior: 'auto' });
      if (!isScrolledToBottom(list)) {
        list.scrollTop = list.scrollHeight;
      }
    });
    this.lastScrolledId = activeId;
    this.atBottom.set(true);
  }

  private scrollListToBottom(): void {
    const list = this.listRef()?.nativeElement;
    if (!list) {
      return;
    }

    this.withProgrammaticScroll(list, () => {
      list.scrollTop = list.scrollHeight;
    });
    this.atBottom.set(true);
  }

  private withProgrammaticScroll(list: HTMLElement, action: () => void): void {
    this.programmaticScroll = true;
    action();
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        this.atBottom.set(isScrolledToBottom(list));
        this.programmaticScroll = false;
      });
    });
  }
}

function isScrolledToBottom(list: HTMLElement): boolean {
  return list.scrollHeight - list.scrollTop - list.clientHeight <= BOTTOM_EPSILON_PX;
}

function severityLabel(severity: number): string {
  if (severity >= 0 && severity < SEVERITY_LABELS.length) {
    return SEVERITY_LABELS[severity]!;
  }
  return 'INFO';
}

/** UTC wall-clock timecode for the message (seconds precision). */
function formatTimecode(changedAtMs: number): string {
  const date = new Date(changedAtMs);
  if (Number.isNaN(date.getTime())) {
    return String(changedAtMs);
  }

  const hours = String(date.getUTCHours()).padStart(2, '0');
  const minutes = String(date.getUTCMinutes()).padStart(2, '0');
  const seconds = String(date.getUTCSeconds()).padStart(2, '0');
  return `${hours}:${minutes}:${seconds}`;
}
