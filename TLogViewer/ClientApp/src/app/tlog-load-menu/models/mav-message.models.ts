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

export interface FlightImuSummary {
  /** Healthy | Warn | Bad | Unknown */
  overallHealth: string;
  overallHealthLabel: string;
  accelAvgMagnitudeG: number | null;
  accelPeakMagnitudeG: number | null;
  accelPeakAbsXG: number | null;
  accelPeakAbsYG: number | null;
  accelPeakAbsZG: number | null;
  accelSampleCount: number;
  accelHealth: string;
  accelHealthLabel: string;
  gyroAvgMagnitudeRadS: number | null;
  gyroPeakMagnitudeRadS: number | null;
  gyroPeakAbsXRadS: number | null;
  gyroPeakAbsYRadS: number | null;
  gyroPeakAbsZRadS: number | null;
  gyroSampleCount: number;
  gyroHealth: string;
  gyroHealthLabel: string;
  vibeAvgMaxMs2: number | null;
  vibePeakMs2: number | null;
  vibePeakXMs2: number | null;
  vibePeakYMs2: number | null;
  vibePeakZMs2: number | null;
  vibeSampleCount: number;
  vibeHealth: string;
  vibeHealthLabel: string;
  clip0Delta: number;
  clip1Delta: number;
  clip2Delta: number;
  clipTotalDelta: number;
  clipSampleCount: number;
  clipHealth: string;
  clipHealthLabel: string;
}

export interface FlightStickChannelUsage {
  channel: number;
  /** Roll | Pitch | Throttle | Yaw */
  name: string;
  fieldKey: string;
  /** Peak |PWM−1500|/500 as % of full stick travel (0–100). */
  usagePercent: number;
  /** Mean |PWM−1500|/500 as % — primary metric (0–100). */
  averageUsagePercent: number;
  /** Good | Improve | Uncontrolled | Unknown */
  usageHealth: string;
  usageHealthLabel: string;
  sampleCount: number;
  pwmMin: number | null;
  pwmMax: number | null;
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
  /** RC stick channels 1–4 usage vs center PWM 1500. */
  stickChannels: FlightStickChannelUsage[];
  /** IMU accel / gyro / vibration / clipping analysis. */
  imu: FlightImuSummary;
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
