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

/** Snap to one decimal place (0.1% grid). */
export function snapProgressPercent(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }
  return Math.min(100, Math.max(0, Math.round(value * 10) / 10));
}
