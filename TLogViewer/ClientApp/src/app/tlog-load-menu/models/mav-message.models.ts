/** Discriminator values returned by the API for parsed MAVLink messages. */
export type MavMessageType =
  | 'heartbeat'
  | 'sysStatus'
  | 'gpsRawInt'
  | 'gpsStatus'
  | 'attitude'
  | 'localPositionNed'
  | 'globalPositionInt'
  | 'missionCurrent'
  | 'gpsGlobalOrigin'
  | 'navControllerOutput'
  | 'rcChannels'
  | 'missionItemInt'
  | 'vfrHud'
  | 'positionTargetGlobalInt'
  | 'homePosition'
  | 'unknown';

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

export interface Flight extends FlightSummary {
  messages: MavMessage[];
}

export interface TlogFlightResult {
  sessionId: string;
  flight: Flight;
  sessionReleased: boolean;
}

export interface MavMessageBase<TType extends MavMessageType, TData> {
  type: TType;
  messageId: string;
  /** TLog trail timestamp as UTC ISO-8601. */
  timeUtc: string;
  data: TData;
}

export interface HeartbeatData {
  customMode: number;
  aircraftType: string;
  autopilot: string;
  systemStatus: string;
  armed: boolean;
  baseMode: number;
  mavlinkVersion: number;
}

export interface SysStatusData {
  load: number;
  batteryVoltageV: number;
  batteryCurrentA: number;
  batteryRemainingPct: number;
  dropRateComm: number;
  errorsComm: number;
  gpsPresent: boolean;
  gpsEnabled: boolean;
  gpsHealthy: boolean;
}

export interface GpsRawIntData {
  fixType: string;
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
  groundSpeedMS: number;
  courseOverGroundDeg: number;
  satellitesVisible: number;
  eph: number;
  epv: number;
}

export interface GpsStatusData {
  satellitesVisible: number;
  satellitePrn: number[];
  satelliteUsed: number[];
  satelliteElevation: number[];
  satelliteAzimuth: number[];
  satelliteSnr: number[];
}

export interface AttitudeData {
  timeBootMs: number;
  rollDeg: number;
  pitchDeg: number;
  yawDeg: number;
  rollSpeed: number;
  pitchSpeed: number;
  yawSpeed: number;
}

export interface LocalPositionNedData {
  timeBootMs: number;
  x: number;
  y: number;
  z: number;
  vx: number;
  vy: number;
  vz: number;
}

export interface GlobalPositionIntData {
  timeBootMs: number;
  latitudeDeg: number;
  longitudeDeg: number;
  aslAltitudeM: number;
  relativeAltitudeM: number;
  horizontalVelocityMS: number;
  verticalVelocityMS: number;
  headingDeg: number;
}

export interface MissionCurrentData {
  seq: number;
  total: number;
  missionState: number;
  missionMode: number;
}

export interface GpsGlobalOriginData {
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
}

export interface NavControllerOutputData {
  navRoll: number;
  navPitch: number;
  navBearing: number;
  targetBearing: number;
  wpDistM: number;
  altErrorM: number;
  aspdErrorMS: number;
  xtrackErrorM: number;
}

export interface RcChannelsData {
  timeBootMs: number;
  channelCount: number;
  channels: number[];
  rssi: number;
}

export interface MissionItemIntData {
  seq: number;
  command: number;
  frame: number;
  current: number;
  autocontinue: number;
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
  param1: number;
  param2: number;
  param3: number;
  param4: number;
}

export interface VfrHudData {
  airspeedMS: number;
  groundSpeedMS: number;
  headingDeg: number;
  throttlePct: number;
  altitudeM: number;
  climbMS: number;
}

export interface PositionTargetGlobalIntData {
  timeBootMs: number;
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
  vx: number;
  vy: number;
  vz: number;
  yaw: number;
  yawRate: number;
  typeMask: number;
  coordinateFrame: number;
}

export interface HomePositionData {
  latitudeDeg: number;
  longitudeDeg: number;
  altitudeM: number;
  x: number;
  y: number;
  z: number;
}

export type MavMessage =
  | MavMessageBase<'heartbeat', HeartbeatData>
  | MavMessageBase<'sysStatus', SysStatusData>
  | MavMessageBase<'gpsRawInt', GpsRawIntData>
  | MavMessageBase<'gpsStatus', GpsStatusData>
  | MavMessageBase<'attitude', AttitudeData>
  | MavMessageBase<'localPositionNed', LocalPositionNedData>
  | MavMessageBase<'globalPositionInt', GlobalPositionIntData>
  | MavMessageBase<'missionCurrent', MissionCurrentData>
  | MavMessageBase<'gpsGlobalOrigin', GpsGlobalOriginData>
  | MavMessageBase<'navControllerOutput', NavControllerOutputData>
  | MavMessageBase<'rcChannels', RcChannelsData>
  | MavMessageBase<'missionItemInt', MissionItemIntData>
  | MavMessageBase<'vfrHud', VfrHudData>
  | MavMessageBase<'positionTargetGlobalInt', PositionTargetGlobalIntData>
  | MavMessageBase<'homePosition', HomePositionData>
  | MavMessageBase<'unknown', Record<string, never>>;

export function isMavMessageType<T extends MavMessageType>(
  message: MavMessage,
  type: T,
): message is Extract<MavMessage, { type: T }> {
  return message.type === type;
}
