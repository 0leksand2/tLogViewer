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

export interface PlaybackHomePoint {
  changedAtMs: number;
  latitudeDeg: number;
  longitudeDeg: number;
}

function asFiniteNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}

/** Build home timeline from flattened HOME_POSITION (242) fields when API omits homePoints. */
export function extractHomePointsFromMessages(
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackHomePoint[] {
  if (!messages) {
    return [];
  }

  const points: PlaybackHomePoint[] = [];
  let lastLat: number | null = null;
  let lastLng: number | null = null;

  for (const changedAtMs of sortedPlaybackPoints(messages)) {
    const fields = messages[String(changedAtMs)];
    if (!fields) {
      continue;
    }

    const latitudeDeg = asFiniteNumber(fields['242_latitudeDeg']);
    const longitudeDeg = asFiniteNumber(fields['242_longitudeDeg']);
    if (latitudeDeg === null || longitudeDeg === null) {
      continue;
    }

    if (Math.abs(latitudeDeg) < 1e-9 && Math.abs(longitudeDeg) < 1e-9) {
      continue;
    }

    if (
      lastLat !== null &&
      lastLng !== null &&
      Math.abs(latitudeDeg - lastLat) < 1e-7 &&
      Math.abs(longitudeDeg - lastLng) < 1e-7
    ) {
      continue;
    }

    points.push({ changedAtMs, latitudeDeg, longitudeDeg });
    lastLat = latitudeDeg;
    lastLng = longitudeDeg;
  }

  return points;
}

export function resolveFlightHomePoints(
  homePoints: readonly PlaybackHomePoint[] | null | undefined,
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackHomePoint[] {
  const resolved = homePoints?.length
    ? [...homePoints]
    : extractHomePointsFromMessages(messages);

  return resolved.sort((a, b) => a.changedAtMs - b.changedAtMs);
}

/** Active home for the current playback millisecond (latest change at or before playback). */
export function resolveActiveHomePoint(
  homePoints: readonly PlaybackHomePoint[] | null | undefined,
  playbackMs: number | null,
): PlaybackHomePoint | null {
  if (!homePoints?.length) {
    return null;
  }

  if (playbackMs === null) {
    return homePoints[0]!;
  }

  let active = homePoints[0]!;
  for (const point of homePoints) {
    if (point.changedAtMs <= playbackMs) {
      active = point;
    } else {
      break;
    }
  }

  return active;
}

/** Plane position and heading at the current playback millisecond. */
export function resolvePlanePosition(
  messages: Record<string, Record<string, unknown>> | null | undefined,
  playbackMs: number | null,
): { lat: number; lon: number; yaw: number | null } | null {
  if (!messages || playbackMs === null) {
    return null;
  }

  const fields = messages[String(playbackMs)];
  if (!fields) {
    return null;
  }

  const lat = asFiniteNumber(fields['lat']);
  const lon = asFiniteNumber(fields['lon']);
  const yaw =
    asFiniteNumber(fields['yaw']) ?? asFiniteNumber(fields['74_headingDeg']);

  if (lat !== null && lon !== null) {
    return { lat, lon, yaw };
  }

  // Fallback for flights processed before lat/lon enrichment.
  for (const [latKey, lonKey] of [
    ['33_latitudeDeg', '33_longitudeDeg'],
    ['24_latitudeDeg', '24_longitudeDeg'],
    ['87_latitudeDeg', '87_longitudeDeg'],
  ] as const) {
    const fallbackLat = asFiniteNumber(fields[latKey]);
    const fallbackLon = asFiniteNumber(fields[lonKey]);
    if (fallbackLat !== null && fallbackLon !== null) {
      return { lat: fallbackLat, lon: fallbackLon, yaw };
    }
  }

  return null;
}
