import { flightModeColor } from '../../core/flight-mode';
import { FlightFieldIds } from '../../core/flight-field-ids';
import {
  DEFAULT_GPS_SOURCE,
  gpsSourceLatLonKeys,
  type MapGpsSource,
} from '../models/map-gps-source';

/** Sample trail vertices every 100 ms of log time. */
export const TRAIL_SAMPLE_MS = 100;

export interface FlightTrailVertex {
  playbackMs: number;
  lat: number;
  lng: number;
  customMode: number;
  /** True when this vertex is exactly at a flight-mode change. */
  isModeChange: boolean;
  color: string;
}

export interface ModeChangeLike {
  changedAtMs: number;
  customMode: number;
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

function findPointIndexAtOrBefore(points: readonly number[], targetMs: number): number {
  if (points.length === 0) {
    return -1;
  }

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

  return points[lo]! <= targetMs ? lo : -1;
}

function readLatLonFromSource(
  fields: Record<string, unknown>,
  gpsSource: MapGpsSource,
): { lat: number; lon: number } | null {
  const { lat: latKey, lon: lonKey } = gpsSourceLatLonKeys(gpsSource);
  const lat = asFiniteNumber(fields[latKey]);
  const lon = asFiniteNumber(fields[lonKey]);
  if (lat === null || lon === null) {
    return null;
  }

  if (Math.abs(lat) < 1e-9 && Math.abs(lon) < 1e-9) {
    return null;
  }

  return { lat, lon };
}

/** Latest plane lat/lon at or before `targetMs` for the selected GPS source. */
export function resolvePlaneLatLonAtOrBefore(
  messages: Record<string, Record<string, unknown>>,
  points: readonly number[],
  targetMs: number,
  gpsSource: MapGpsSource = DEFAULT_GPS_SOURCE,
): { lat: number; lon: number } | null {
  const start = findPointIndexAtOrBefore(points, targetMs);
  if (start < 0) {
    return null;
  }

  for (let i = start; i >= 0; i--) {
    const fields = messages[String(points[i])];
    if (!fields) {
      continue;
    }
    const pos = readLatLonFromSource(fields, gpsSource);
    if (pos) {
      return pos;
    }
  }

  return null;
}

/** Latest HEARTBEAT customMode at or before `targetMs`. */
export function resolveFlightModeAtOrBefore(
  messages: Record<string, Record<string, unknown>>,
  points: readonly number[],
  targetMs: number,
): number | null {
  const start = findPointIndexAtOrBefore(points, targetMs);
  if (start < 0) {
    return null;
  }

  for (let i = start; i >= 0; i--) {
    const fields = messages[String(points[i])];
    if (!fields) {
      continue;
    }
    const mode = asFiniteNumber(fields[FlightFieldIds.CustomMode]);
    if (mode !== null) {
      return Math.trunc(mode);
    }
  }

  return null;
}

/**
 * Builds trail vertices for the window ending at `playbackMs`:
 * one sample every 100 ms of log time, plus forced vertices at mode changes.
 * When `fullTrail` is true, the window starts at the first flight message.
 */
export function buildFlightTrail(
  messages: Record<string, Record<string, unknown>> | null | undefined,
  playbackPoints: readonly number[],
  playbackMs: number | null,
  modeChangePoints: readonly ModeChangeLike[],
  trailLengthSeconds: number,
  fullTrail = false,
  gpsSource: MapGpsSource = DEFAULT_GPS_SOURCE,
): FlightTrailVertex[] {
  if (!messages || playbackMs === null || playbackPoints.length === 0) {
    return [];
  }

  const flightStartMs = playbackPoints[0]!;
  const windowStart = fullTrail
    ? flightStartMs
    : Math.max(flightStartMs, playbackMs - Math.max(1, trailLengthSeconds) * 1000);
  const times = new Set<number>();

  const firstSample = Math.ceil(windowStart / TRAIL_SAMPLE_MS) * TRAIL_SAMPLE_MS;
  for (let t = firstSample; t <= playbackMs; t += TRAIL_SAMPLE_MS) {
    if (t >= windowStart) {
      times.add(t);
    }
  }

  const modeChangeMs = new Set<number>();
  for (const change of modeChangePoints) {
    if (change.changedAtMs >= windowStart && change.changedAtMs <= playbackMs) {
      times.add(change.changedAtMs);
      modeChangeMs.add(change.changedAtMs);
    }
  }

  const sortedTimes = [...times].sort((a, b) => a - b);
  const vertices: FlightTrailVertex[] = [];
  let lastLat: number | null = null;
  let lastLng: number | null = null;

  for (const t of sortedTimes) {
    const pos = resolvePlaneLatLonAtOrBefore(messages, playbackPoints, t, gpsSource);
    if (!pos) {
      continue;
    }

    const customMode = resolveFlightModeAtOrBefore(messages, playbackPoints, t);
    if (customMode === null) {
      continue;
    }

    // Skip duplicate coordinates unless this is a mode-change marker.
    const isModeChange = modeChangeMs.has(t);
    if (
      !isModeChange &&
      lastLat !== null &&
      lastLng !== null &&
      Math.abs(pos.lat - lastLat) < 1e-9 &&
      Math.abs(pos.lon - lastLng) < 1e-9 &&
      vertices.length > 0
    ) {
      // Still advance last sample time conceptually by replacing tip mode/time.
      const tip = vertices[vertices.length - 1]!;
      tip.playbackMs = t;
      tip.customMode = customMode;
      tip.color = flightModeColor(customMode);
      continue;
    }

    vertices.push({
      playbackMs: t,
      lat: pos.lat,
      lng: pos.lon,
      customMode,
      isModeChange,
      color: flightModeColor(customMode),
    });

    lastLat = pos.lat;
    lastLng = pos.lon;
  }

  return vertices;
}
