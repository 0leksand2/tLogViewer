import { FlightFieldIds } from '../../core/flight-field-ids';

/** MAVLink message used as the aircraft map position source. */
export type MapGpsSource =
  | 'GLOBAL_POSITION_INT'
  | 'GPS_RAW_INT'
  | 'GPS_INPUT'
  | 'AHRS2';

export const GPS_SOURCE_OPTIONS: ReadonlyArray<{ value: MapGpsSource; label: string }> = [
  { value: 'GLOBAL_POSITION_INT', label: 'GLOBAL_POSITION_INT' },
  { value: 'GPS_RAW_INT', label: 'GPS_RAW_INT' },
  { value: 'GPS_INPUT', label: 'GPS_INPUT' },
  { value: 'AHRS2', label: 'AHRS2' },
];

export const DEFAULT_GPS_SOURCE: MapGpsSource = 'GLOBAL_POSITION_INT';

const GPS_SOURCE_KEYS: Readonly<Record<MapGpsSource, { lat: string; lon: string }>> = {
  GLOBAL_POSITION_INT: {
    lat: FlightFieldIds.GlobalPosLat,
    lon: FlightFieldIds.GlobalPosLon,
  },
  GPS_RAW_INT: {
    lat: FlightFieldIds.GpsRawLat,
    lon: FlightFieldIds.GpsRawLon,
  },
  GPS_INPUT: {
    lat: FlightFieldIds.GpsInputLat,
    lon: FlightFieldIds.GpsInputLon,
  },
  AHRS2: {
    lat: FlightFieldIds.Ahrs2Lat,
    lon: FlightFieldIds.Ahrs2Lng,
  },
};

export function isMapGpsSource(value: unknown): value is MapGpsSource {
  return (
    value === 'GLOBAL_POSITION_INT' ||
    value === 'GPS_RAW_INT' ||
    value === 'GPS_INPUT' ||
    value === 'AHRS2'
  );
}

export function gpsSourceLatLonKeys(source: MapGpsSource): { lat: string; lon: string } {
  return GPS_SOURCE_KEYS[source];
}
