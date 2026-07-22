/** ArduPlane HEARTBEAT customMode labels (see FlightMode enum). */
export const FLIGHT_MODE_NAMES: Readonly<Record<number, string>> = {
  0: 'MANUAL',
  1: 'CIRCLE',
  2: 'STABILIZE',
  3: 'TRAINING',
  4: 'ACRO',
  5: 'FBWA',
  6: 'FBWB',
  7: 'CRUISE',
  8: 'AUTOTUNE',
  10: 'AUTO',
  11: 'RTL',
  12: 'LOITER',
  15: 'GUIDED',
  16: 'INITIALIZING',
  17: 'QSTABILIZE',
  18: 'QHOVER',
  19: 'QLOITER',
  20: 'QLAND',
  21: 'QRTL',
};

/** Distinct trail/map colors per flight mode. */
export const FLIGHT_MODE_COLORS: Readonly<Record<number, string>> = {
  0: '#ef4444',
  1: '#f59e0b',
  2: '#eab308',
  3: '#84cc16',
  4: '#22c55e',
  5: '#14b8a6',
  6: '#06b6d4',
  7: '#0ea5e9',
  8: '#3b82f6',
  10: '#6366f1',
  11: '#8b5cf6',
  12: '#a855f7',
  15: '#d946ef',
  16: '#94a3b8',
  17: '#f43f5e',
  18: '#fb7185',
  19: '#fdba74',
  20: '#fbbf24',
  21: '#a3e635',
};

const DEFAULT_MODE_COLOR = '#64748b';

export function flightModeLabel(customMode: number): string {
  return FLIGHT_MODE_NAMES[customMode] ?? String(customMode);
}

export function flightModeColor(customMode: number): string {
  return FLIGHT_MODE_COLORS[customMode] ?? DEFAULT_MODE_COLOR;
}

export interface FlightModeLegendEntry {
  customMode: number;
  label: string;
  color: string;
}

/** Ordered legend entries for trail color explanations. */
export const FLIGHT_MODE_LEGEND: readonly FlightModeLegendEntry[] = Object.keys(FLIGHT_MODE_NAMES)
  .map(Number)
  .sort((a, b) => a - b)
  .map((customMode) => ({
    customMode,
    label: flightModeLabel(customMode),
    color: flightModeColor(customMode),
  }));
