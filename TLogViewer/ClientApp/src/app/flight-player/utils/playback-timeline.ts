/** Sorted Unix-ms keys from a flight messages dictionary. */
export function sortedPlaybackPoints(
  messages: Record<string, Record<string, unknown>> | null | undefined,
): number[] {
  if (!messages) {
    return [];
  }

  return Object.keys(messages)
    .map(Number)
    .filter((ms) => Number.isFinite(ms))
    .sort((a, b) => a - b);
}

/**
 * Maps 0–100% progress onto the timeline spanned by playback points,
 * returning the latest point at or before that instant.
 */
export function resolvePlaybackPoint(points: number[], progressPercent: number): number | null {
  if (points.length === 0) {
    return null;
  }

  if (points.length === 1) {
    return points[0]!;
  }

  const clamped = Math.min(100, Math.max(0, progressPercent));
  const first = points[0]!;
  const last = points[points.length - 1]!;
  const targetMs = first + (last - first) * (clamped / 100);

  let lo = 0;
  let hi = points.length - 1;
  while (lo < hi) {
    const mid = Math.ceil((lo + hi + 1) / 2);
    if (points[mid]! <= targetMs) {
      lo = mid;
    } else {
      hi = mid - 1;
    }
  }

  return points[lo]!;
}

/** Snap to one decimal place (0.1% grid). */
export function snapProgressPercent(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }
  return Math.min(100, Math.max(0, Math.round(value * 10) / 10));
}
