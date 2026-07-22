import { FlightFieldIds } from '../../core/flight-field-ids';

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
  altitudeM: number | null;
}

function asFiniteNumber(value: unknown): number | null {
  if (typeof value === 'number' && Number.isFinite(value)) {
    return value;
  }

  if (typeof value === 'string' && value.trim() !== '') {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  return null;
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

    const latitudeDeg = asFiniteNumber(fields[FlightFieldIds.HomeLatitudeDeg]);
    const longitudeDeg = asFiniteNumber(fields[FlightFieldIds.HomeLongitudeDeg]);
    const altitudeM = asFiniteNumber(fields[FlightFieldIds.HomeAltitudeM]);
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

    points.push({ changedAtMs, latitudeDeg, longitudeDeg, altitudeM });
    lastLat = latitudeDeg;
    lastLng = longitudeDeg;
  }

  return points;
}

export function resolveFlightHomePoints(
  homePoints: readonly PlaybackHomePoint[] | null | undefined,
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackHomePoint[] {
  const fromApi = (homePoints ?? [])
    .map((point) => {
      const raw = point as {
        changedAtMs?: unknown;
        latitudeDeg?: unknown;
        longitudeDeg?: unknown;
        altitudeM?: unknown;
      };
      return {
        changedAtMs: asFiniteNumber(raw.changedAtMs) ?? NaN,
        latitudeDeg: asFiniteNumber(raw.latitudeDeg) ?? NaN,
        longitudeDeg: asFiniteNumber(raw.longitudeDeg) ?? NaN,
        altitudeM: asFiniteNumber(raw.altitudeM),
      };
    })
    .filter(
      (point) =>
        Number.isFinite(point.changedAtMs) &&
        Number.isFinite(point.latitudeDeg) &&
        Number.isFinite(point.longitudeDeg),
    );

  const resolved = fromApi.length > 0 ? fromApi : extractHomePointsFromMessages(messages);

  return resolved.sort((a, b) => a.changedAtMs - b.changedAtMs);
}

export interface PlaybackModeChangePoint {
  changedAtMs: number;
  customMode: number;
}

/** Build mode-change timeline from flattened HEARTBEAT customMode when API omits points. */
export function extractModeChangePointsFromMessages(
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackModeChangePoint[] {
  if (!messages) {
    return [];
  }

  const points: PlaybackModeChangePoint[] = [];
  let lastMode: number | null = null;

  for (const changedAtMs of sortedPlaybackPoints(messages)) {
    const fields = messages[String(changedAtMs)];
    if (!fields) {
      continue;
    }

    const customMode = asFiniteNumber(fields[FlightFieldIds.CustomMode]);
    if (customMode === null) {
      continue;
    }

    const mode = Math.trunc(customMode);
    if (lastMode !== null && mode !== lastMode) {
      points.push({ changedAtMs, customMode: mode });
    }

    lastMode = mode;
  }

  return points;
}

export function resolveFlightModeChangePoints(
  modeChangePoints: readonly PlaybackModeChangePoint[] | null | undefined,
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackModeChangePoint[] {
  const fromApi = (modeChangePoints ?? [])
    .map((point) => {
      const raw = point as { changedAtMs?: unknown; customMode?: unknown };
      return {
        changedAtMs: asFiniteNumber(raw.changedAtMs) ?? NaN,
        customMode: asFiniteNumber(raw.customMode) ?? NaN,
      };
    })
    .filter(
      (point) => Number.isFinite(point.changedAtMs) && Number.isFinite(point.customMode),
    )
    .map((point) => ({
      changedAtMs: point.changedAtMs,
      customMode: Math.trunc(point.customMode),
    }));

  const resolved =
    fromApi.length > 0 ? fromApi : extractModeChangePointsFromMessages(messages);

  return resolved.sort((a, b) => a.changedAtMs - b.changedAtMs);
}

export interface PlaybackArmChangePoint {
  changedAtMs: number;
  armed: boolean;
}

/** Build arm/disarm timeline from flattened HEARTBEAT armed when API omits points. */
export function extractArmChangePointsFromMessages(
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackArmChangePoint[] {
  if (!messages) {
    return [];
  }

  const points: PlaybackArmChangePoint[] = [];
  let lastArmed: boolean | null = null;

  for (const changedAtMs of sortedPlaybackPoints(messages)) {
    const fields = messages[String(changedAtMs)];
    if (!fields) {
      continue;
    }

    const armed = asBoolean(fields[FlightFieldIds.Armed]);
    if (armed === null) {
      continue;
    }

    if (lastArmed !== null && armed !== lastArmed) {
      points.push({ changedAtMs, armed });
    }

    lastArmed = armed;
  }

  return points;
}

export function resolveFlightArmChangePoints(
  armChangePoints: readonly PlaybackArmChangePoint[] | null | undefined,
  messages: Record<string, Record<string, unknown>> | null | undefined,
): PlaybackArmChangePoint[] {
  const fromApi = (armChangePoints ?? [])
    .map((point) => {
      const raw = point as { changedAtMs?: unknown; armed?: unknown };
      return {
        changedAtMs: asFiniteNumber(raw.changedAtMs) ?? NaN,
        armed: asBoolean(raw.armed),
      };
    })
    .filter(
      (point): point is { changedAtMs: number; armed: boolean } =>
        Number.isFinite(point.changedAtMs) && point.armed !== null,
    );

  const resolved =
    fromApi.length > 0 ? fromApi : extractArmChangePointsFromMessages(messages);

  return resolved.sort((a, b) => a.changedAtMs - b.changedAtMs);
}

function asBoolean(value: unknown): boolean | null {
  if (typeof value === 'boolean') {
    return value;
  }
  if (value === 0 || value === '0' || value === 'false') {
    return false;
  }
  if (value === 1 || value === '1' || value === 'true') {
    return true;
  }
  return null;
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

/** Plane position and headings at the current playback millisecond. */
export function resolvePlanePosition(
  messages: Record<string, Record<string, unknown>> | null | undefined,
  playbackMs: number | null,
): {
  lat: number;
  lon: number;
  yaw: number | null;
  navBearing: number | null;
  windDir: number | null;
  windSpeed: number | null;
} | null {
  if (!messages || playbackMs === null) {
    return null;
  }

  const fields = messages[String(playbackMs)];
  if (!fields) {
    return null;
  }

  const lat = asFiniteNumber(fields[FlightFieldIds.AliasLat]);
  const lon = asFiniteNumber(fields[FlightFieldIds.AliasLon]);
  const yaw =
    asFiniteNumber(fields[FlightFieldIds.AliasYaw]) ??
    asFiniteNumber(fields[FlightFieldIds.VfrHeadingDeg]);
  const navBearing =
    asFiniteNumber(fields[FlightFieldIds.AliasNavBearing]) ??
    asFiniteNumber(fields[FlightFieldIds.NavBearing]);
  const windDir =
    asFiniteNumber(fields[FlightFieldIds.AliasWindDir]) ??
    asFiniteNumber(fields[FlightFieldIds.WindDirection]);
  const windSpeed =
    asFiniteNumber(fields[FlightFieldIds.AliasWindSpeed]) ??
    asFiniteNumber(fields[FlightFieldIds.WindSpeed]);

  if (lat !== null && lon !== null) {
    return { lat, lon, yaw, navBearing, windDir, windSpeed };
  }

  // Fallback for flights processed before lat/lon enrichment.
  for (const [latKey, lonKey] of [
    [FlightFieldIds.GlobalPosLat, FlightFieldIds.GlobalPosLon],
    [FlightFieldIds.GpsRawLat, FlightFieldIds.GpsRawLon],
    [FlightFieldIds.PositionTargetLat, FlightFieldIds.PositionTargetLon],
  ] as const) {
    const fallbackLat = asFiniteNumber(fields[latKey]);
    const fallbackLon = asFiniteNumber(fields[lonKey]);
    if (fallbackLat !== null && fallbackLon !== null) {
      return { lat: fallbackLat, lon: fallbackLon, yaw, navBearing, windDir, windSpeed };
    }
  }

  return null;
}

/** Active navigation/position target at the current playback millisecond. */
export function resolvePositionTarget(
  messages: Record<string, Record<string, unknown>> | null | undefined,
  playbackMs: number | null,
): { lat: number; lon: number; yaw: number | null; altitudeM: number | null } | null {
  if (!messages || playbackMs === null) {
    return null;
  }

  const fields = messages[String(playbackMs)];
  if (!fields) {
    return null;
  }

  const lat =
    asFiniteNumber(fields[FlightFieldIds.AliasTargetLat]) ??
    asFiniteNumber(fields[FlightFieldIds.PositionTargetLat]);
  const lon =
    asFiniteNumber(fields[FlightFieldIds.AliasTargetLon]) ??
    asFiniteNumber(fields[FlightFieldIds.PositionTargetLon]);
  const yaw =
    asFiniteNumber(fields[FlightFieldIds.AliasTargetYaw]) ??
    asFiniteNumber(fields[FlightFieldIds.AliasTargetBearing]) ??
    asFiniteNumber(fields[FlightFieldIds.PositionTargetYaw]) ??
    asFiniteNumber(fields[FlightFieldIds.NavTargetBearing]);
  const altitudeM =
    asFiniteNumber(fields[FlightFieldIds.AliasTargetAlt]) ??
    asFiniteNumber(fields[FlightFieldIds.PositionTargetAlt]);

  if (lat === null || lon === null) {
    return null;
  }

  if (Math.abs(lat) < 1e-9 && Math.abs(lon) < 1e-9) {
    return null;
  }

  return { lat, lon, yaw, altitudeM };
}

const SPARSE_DERIVED_KEYS = [
  FlightFieldIds.LinkQualityGcs,
  FlightFieldIds.TimeSinceArmSec,
] as const;

/**
 * Copies the latest DERIVED (998) fields at or before `playbackMs` into `values`
 * when the current millisecond bucket does not include them (sparse 1 Hz samples).
 */
export function ensureDerivedPlaybackValues(
  messages: Record<string, Record<string, unknown>> | null | undefined,
  points: readonly number[],
  playbackMs: number | null,
  values: Record<string, unknown>,
): Record<string, unknown> {
  if (!messages || playbackMs === null || points.length === 0) {
    return values;
  }

  const missing = SPARSE_DERIVED_KEYS.filter((key) => values[key] === undefined);
  if (missing.length === 0) {
    return values;
  }

  // points are sorted; find last index with ms <= playbackMs
  let lo = 0;
  let hi = points.length - 1;
  while (lo < hi) {
    const mid = Math.ceil((lo + hi + 1) / 2);
    if (points[mid]! <= playbackMs) {
      lo = mid;
    } else {
      hi = mid - 1;
    }
  }

  const filled = { ...values };
  const stillMissing = new Set(missing);

  for (let i = lo; i >= 0 && stillMissing.size > 0; i--) {
    const fields = messages[String(points[i])];
    if (!fields) {
      continue;
    }

    for (const key of stillMissing) {
      if (fields[key] !== undefined) {
        filled[key] = fields[key];
        stillMissing.delete(key);
      }
    }
  }

  return filled;
}

