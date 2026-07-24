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

export interface FlightSpoofEvent {
  timestampMs: number;
  timestampUtc: string;
  fromLatitudeDeg: number;
  fromLongitudeDeg: number;
  toLatitudeDeg: number;
  toLongitudeDeg: number;
  distanceM: number;
}

export interface FlightMagRadiationEvent {
  timestampMs: number;
  timestampUtc: string;
  fieldName: string;
  jumpPoints: number;
  latitudeDeg: number | null;
  longitudeDeg: number | null;
}

export interface FlightSummaryReport {
  gpsExists: boolean;
  maxSatCount: number;
  hdop: number | null;
  hdopMin: number | null;
  hdopMax: number | null;
  hdopSampleCount: number;
  /** Unhealthy | PossiblyUnhealthy | Healthy | Unknown */
  hdopHealth: string;
  hdopHealthLabel: string;
  spoofDetected: boolean;
  spoofEvents: FlightSpoofEvent[];
  strongMagneticRadiationDetected: boolean;
  magRadiationEvents: FlightMagRadiationEvent[];
  moveMagnetometerAwayFromMotor: boolean;
  magThrottleCorrelation: number | null;
  yawErrorGrowing: boolean;
  yawErrorAverageDeg: number | null;
  /** Good | Ok | Bad | Unknown */
  yawCogHealth: string;
  yawCogHealthLabel: string;
  yawCogDiffAverageDeg: number | null;
  yawCogSampleCount: number;
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
  /** GPS / HDOP / spoof analysis from the server. */
  summaryReport?: FlightSummaryReport;
}

export interface TlogFlightResult {
  sessionId: string;
  flight: Flight;
  sessionReleased: boolean;
}
