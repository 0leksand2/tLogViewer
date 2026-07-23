export interface TlogUploadResult {
  sessionId: string;
  fileName: string;
  size: number;
  totalRecords: number;
  parsedCount: number;
  flightCount: number;
  /** Summaries only — fetch messages via getFlight(). */
  flights: FlightSummary[];
}

export interface FlightSummary {
  id: string;
  startTimeUtc: string;
  endTimeUtc: string;
  armedFromTimeUtc: string;
  armedUntilTimeUtc: string;
  durationSeconds: number;
  messageCount: number;
}

export interface FlightHomePoint {
  changedAtMs: number;
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
}

export interface FlightModeChangePoint {
  changedAtMs: number;
  customMode: number;
}

export interface FlightArmChangePoint {
  changedAtMs: number;
  /** true = armed, false = disarmed. */
  armed: boolean;
}

export interface FlightStatusText {
  severity: number;
  text: string;
}

export interface Flight extends FlightSummary {
  /** Unix ms → ({messageId}_{valueName} → field value). */
  messages: Record<string, Record<string, unknown>>;
  homePoints: FlightHomePoint[];
  /** Unix ms when HEARTBEAT customMode changed (from log analysis). */
  modeChangePoints?: FlightModeChangePoint[];
  /** Unix ms when HEARTBEAT armed state changed (arm / disarm). */
  armChangePoints?: FlightArmChangePoint[];
  /** STATUSTEXT lines keyed by Unix ms (separate from telemetry messages). */
  statusTexts?: Record<string, FlightStatusText[]>;
}

export interface TlogFlightResult {
  sessionId: string;
  flight: Flight;
  sessionReleased: boolean;
}
