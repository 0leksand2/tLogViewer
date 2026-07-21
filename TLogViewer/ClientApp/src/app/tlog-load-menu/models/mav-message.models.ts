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

export interface Flight extends FlightSummary {
  /** Unix ms → ({messageId}_{valueName} → field value). */
  messages: Record<string, Record<string, unknown>>;
  homePoints: FlightHomePoint[];
}

export interface TlogFlightResult {
  sessionId: string;
  flight: Flight;
  sessionReleased: boolean;
}
