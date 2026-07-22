import {
  afterNextRender,
  Component,
  effect,
  ElementRef,
  inject,
  OnDestroy,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';
import { MapDisplaySettingsService } from '../services/map-display-settings.service';
import { FlightTrailVertex } from '../utils/flight-trail';
import { flightModeLabel } from '../../core/flight-mode';

export interface HomeLocationOptions {
  /** Pan/zoom the map when the active flight changes. */
  recenter?: boolean;
}

@Component({
  selector: 'app-map',
  standalone: true,
  templateUrl: './map.html',
  styleUrl: './map.scss',
})
export class MapComponent implements OnDestroy {
  private readonly mapContainer = viewChild.required<ElementRef<HTMLDivElement>>('mapContainer');
  private readonly displaySettings = inject(MapDisplaySettingsService);
  private map?: L.Map;
  private homeMarker?: L.Marker;
  private planeMarker?: L.Marker;
  private targetMarker?: L.Marker;
  private pendingHome: {
    lat: number;
    lng: number;
    altitudeM: number | null;
    recenter: boolean;
    flightKey: string | null;
  } | null = null;
  private pendingPlane: {
    lat: number;
    lng: number;
    yaw: number | null;
    navBearing: number | null;
    windDir: number | null;
    windSpeed: number | null;
  } | null = null;
  private pendingTarget: { lat: number; lng: number; altitudeM: number | null } | null = null;
  private lastRecenterFlightKey: string | null = null;
  private lastHomeState: { lat: number; lng: number; altitudeM: number | null } | null = null;
  private lastHomeAltitudeM: number | null = null;
  private lastPlaneState: {
    lat: number;
    lng: number;
    yaw: number | null;
    navBearing: number | null;
    windDir: number | null;
    windSpeed: number | null;
  } | null = null;
  private lastAppliedYaw: number | null = null;
  private lastAppliedNavBearing: number | null = null;
  private lastAppliedWindDir: number | null = null;
  private lastAppliedWindSpeed: number | null = null;
  private lastPlaneFlightKey: string | null = null;
  private lastTargetState: { lat: number; lng: number; altitudeM: number | null } | null =
    null;
  private lastTargetAltitudeM: number | null = null;
  private planeIconBody?: HTMLElement;
  private planeHeadingLine?: HTMLElement;
  private planeNavBearingLayer?: HTMLElement;
  private planeWindLayer?: HTMLElement;
  private planeWindLine?: HTMLElement;
  private planeWindArrow?: HTMLElement;
  private trailLayer?: L.LayerGroup;
  private lastTrailSignature = '';

  constructor() {
    afterNextRender(() => this.initMap());

    effect(() => {
      this.displaySettings.displayHeading();
      this.displaySettings.displayTargetPath();
      this.displaySettings.displayWind();
      this.applyDisplayLineVisibility();
    });

    effect(() => {
      if (!this.displaySettings.displayTrail()) {
        this.clearFlightTrail();
      }
    });
  }

  ngOnDestroy(): void {
    this.clearFlightTrail();
    this.homeMarker = undefined;
    this.planeMarker = undefined;
    this.targetMarker = undefined;
    this.map?.remove();
    this.map = undefined;
  }

  invalidateSize(): void {
    requestAnimationFrame(() => this.map?.invalidateSize());
  }

  /** Show or hide the home pin (letter H). Pass null to clear. */
  setHomeLocation(
    latitudeDeg: number | null,
    longitudeDeg: number | null,
    flightKey: string | null = null,
    options: HomeLocationOptions = {},
    altitudeM: number | null = null,
  ): void {
    if (
      latitudeDeg === null ||
      longitudeDeg === null ||
      !Number.isFinite(latitudeDeg) ||
      !Number.isFinite(longitudeDeg)
    ) {
      this.pendingHome = null;
      this.lastRecenterFlightKey = null;
      this.lastHomeState = null;
      this.lastHomeAltitudeM = null;
      this.homeMarker?.unbindTooltip();
      this.homeMarker?.setOpacity(0);
      return;
    }

    const recenter =
      options.recenter === true &&
      flightKey !== null &&
      flightKey !== this.lastRecenterFlightKey;

    if (!this.map) {
      this.pendingHome = {
        lat: latitudeDeg,
        lng: longitudeDeg,
        altitudeM,
        recenter,
        flightKey,
      };
      return;
    }

    this.applyHomeMarker(latitudeDeg, longitudeDeg, recenter, flightKey, altitudeM);
  }

  /** Show or hide the drone at the current playback position. Pass null to clear. */
  setPlaneLocation(
    latitudeDeg: number | null,
    longitudeDeg: number | null,
    yawDeg: number | null = null,
    flightKey: string | null = null,
    navBearingDeg: number | null = null,
    windDirDeg: number | null = null,
    windSpeedMS: number | null = null,
  ): void {
    if (
      latitudeDeg === null ||
      longitudeDeg === null ||
      !Number.isFinite(latitudeDeg) ||
      !Number.isFinite(longitudeDeg)
    ) {
      this.pendingPlane = null;
      this.resetPlaneMarkerState();
      this.clearFlightTrail();
      return;
    }

    if (flightKey !== this.lastPlaneFlightKey) {
      this.resetPlaneMarkerState({ keepFlightKey: true });
      this.clearFlightTrail();
      this.lastPlaneFlightKey = flightKey;
    }

    if (!this.map) {
      this.pendingPlane = {
        lat: latitudeDeg,
        lng: longitudeDeg,
        yaw: yawDeg,
        navBearing: navBearingDeg,
        windDir: windDirDeg,
        windSpeed: windSpeedMS,
      };
      return;
    }

    this.applyPlaneMarker(
      latitudeDeg,
      longitudeDeg,
      yawDeg,
      navBearingDeg,
      windDirDeg,
      windSpeedMS,
    );
  }

  /** Show or hide the POSITION_TARGET_GLOBAL_INT setpoint. Pass null to clear. */
  setTargetLocation(
    latitudeDeg: number | null,
    longitudeDeg: number | null,
    altitudeM: number | null = null,
  ): void {
    if (
      latitudeDeg === null ||
      longitudeDeg === null ||
      !Number.isFinite(latitudeDeg) ||
      !Number.isFinite(longitudeDeg)
    ) {
      this.pendingTarget = null;
      this.lastTargetState = null;
      this.lastTargetAltitudeM = null;
      this.targetMarker?.unbindTooltip();
      this.targetMarker?.setOpacity(0);
      return;
    }

    if (!this.map) {
      this.pendingTarget = { lat: latitudeDeg, lng: longitudeDeg, altitudeM };
      return;
    }

    this.applyTargetMarker(latitudeDeg, longitudeDeg, altitudeM);
  }

  /** Replace the mode-colored flight trail (empty clears). */
  setFlightTrail(vertices: readonly FlightTrailVertex[]): void {
    if (!this.displaySettings.displayTrail()) {
      this.clearFlightTrail();
      return;
    }

    const signature = vertices
      .map(
        (vertex) =>
          `${vertex.playbackMs}:${vertex.lat.toFixed(6)}:${vertex.lng.toFixed(6)}:${vertex.customMode}:${vertex.isModeChange ? 1 : 0}`,
      )
      .join('|');

    if (signature === this.lastTrailSignature) {
      return;
    }

    this.lastTrailSignature = signature;

    if (!this.map) {
      return;
    }

    this.ensureTrailLayer();
    this.trailLayer!.clearLayers();

    if (vertices.length === 0) {
      return;
    }

    // Segment polylines by contiguous customMode. Include the next (mode-change)
    // vertex on the ending segment so the old color meets the change dot.
    let segmentStart = 0;
    for (let i = 1; i <= vertices.length; i++) {
      const prev = vertices[i - 1]!;
      const next = vertices[i];
      const modeBreak = !next || next.customMode !== prev.customMode;

      if (!modeBreak) {
        continue;
      }

      const segment = vertices.slice(segmentStart, i);
      const lineVertices = next ? [...segment, next] : segment;
      if (lineVertices.length >= 2) {
        L.polyline(
          lineVertices.map((vertex) => [vertex.lat, vertex.lng] as L.LatLngExpression),
          {
            color: segment[0]?.color ?? prev.color,
            weight: 3,
            opacity: 0.85,
            lineJoin: 'round',
            lineCap: 'round',
          },
        ).addTo(this.trailLayer!);
      }

      segmentStart = i;
    }

    for (const vertex of vertices) {
      const radius = vertex.isModeChange ? 3 : 1.25;
      const marker = L.circleMarker([vertex.lat, vertex.lng], {
        radius,
        color: vertex.color,
        weight: vertex.isModeChange ? 1.5 : 1,
        fillColor: vertex.color,
        fillOpacity: vertex.isModeChange ? 1 : 0.85,
        opacity: 1,
      });

      if (vertex.isModeChange) {
        marker.bindTooltip(`FLT MODE: ${flightModeLabel(vertex.customMode)}`, {
          direction: 'top',
          opacity: 0.9,
        });
      }

      marker.addTo(this.trailLayer!);
    }
  }

  clearFlightTrail(): void {
    this.lastTrailSignature = '';
    this.trailLayer?.clearLayers();
  }

  private ensureTrailLayer(): void {
    if (!this.map) {
      return;
    }

    if (!this.trailLayer) {
      this.trailLayer = L.layerGroup().addTo(this.map);
    }
  }

  private resetPlaneMarkerState(options: { keepFlightKey?: boolean } = {}): void {
    if (!options.keepFlightKey) {
      this.lastPlaneFlightKey = null;
    }
    this.lastPlaneState = null;
    this.lastAppliedYaw = null;
    this.lastAppliedNavBearing = null;
    this.lastAppliedWindDir = null;
    this.lastAppliedWindSpeed = null;
    this.planeIconBody = undefined;
    this.planeHeadingLine = undefined;
    this.planeNavBearingLayer = undefined;
    this.planeWindLayer = undefined;
    this.planeWindLine = undefined;
    this.planeWindArrow = undefined;
    this.planeMarker?.setOpacity(0);
    this.planeMarker?.remove();
    this.planeMarker = undefined;
  }

  private initMap(): void {
    const container = this.mapContainer().nativeElement;

    delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
    L.Icon.Default.mergeOptions({
      iconRetinaUrl: 'leaflet/images/marker-icon-2x.png',
      iconUrl: 'leaflet/images/marker-icon.png',
      shadowUrl: 'leaflet/images/marker-shadow.png',
    });

    this.map = L.map(container, {
      center: [48.45, 31.5],
      zoom: 6,
      zoomControl: true,
    });

    const attribution =
      'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community';

    L.tileLayer(
      'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      {
        maxZoom: 19,
        attribution,
      },
    ).addTo(this.map);

    L.tileLayer(
      'https://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}',
      {
        maxZoom: 19,
        attribution,
        pane: 'overlayPane',
      },
    ).addTo(this.map);

    if (this.pendingHome) {
      const pending = this.pendingHome;
      this.pendingHome = null;
      this.applyHomeMarker(
        pending.lat,
        pending.lng,
        pending.recenter,
        pending.flightKey,
        pending.altitudeM,
      );
    }

    if (this.pendingPlane) {
      const pending = this.pendingPlane;
      this.pendingPlane = null;
      this.applyPlaneMarker(
        pending.lat,
        pending.lng,
        pending.yaw,
        pending.navBearing,
        pending.windDir,
        pending.windSpeed,
      );
    }

    this.ensureTrailLayer();

    if (this.pendingTarget) {
      const pending = this.pendingTarget;
      this.pendingTarget = null;
      this.applyTargetMarker(pending.lat, pending.lng, pending.altitudeM);
    }

    requestAnimationFrame(() => this.map?.invalidateSize());
  }

  private applyPlaneMarker(
    latitudeDeg: number,
    longitudeDeg: number,
    yawDeg: number | null,
    navBearingDeg: number | null,
    windDirDeg: number | null,
    windSpeedMS: number | null,
  ): void {
    if (!this.map) {
      return;
    }

    const effectiveYaw = yawDeg ?? this.lastAppliedYaw;
    if (effectiveYaw !== null && Number.isFinite(effectiveYaw)) {
      this.lastAppliedYaw = effectiveYaw;
    }

    const effectiveNavBearing = navBearingDeg ?? this.lastAppliedNavBearing;
    if (effectiveNavBearing !== null && Number.isFinite(effectiveNavBearing)) {
      this.lastAppliedNavBearing = effectiveNavBearing;
    }

    const effectiveWindDir = windDirDeg ?? this.lastAppliedWindDir;
    if (effectiveWindDir !== null && Number.isFinite(effectiveWindDir)) {
      this.lastAppliedWindDir = effectiveWindDir;
    }

    const effectiveWindSpeed = windSpeedMS ?? this.lastAppliedWindSpeed;
    if (effectiveWindSpeed !== null && Number.isFinite(effectiveWindSpeed)) {
      this.lastAppliedWindSpeed = effectiveWindSpeed;
    }

    if (
      this.planeMarker &&
      this.lastPlaneState &&
      this.lastPlaneState.lat === latitudeDeg &&
      this.lastPlaneState.lng === longitudeDeg &&
      this.lastPlaneState.yaw === effectiveYaw &&
      this.lastPlaneState.navBearing === effectiveNavBearing &&
      this.lastPlaneState.windDir === effectiveWindDir &&
      this.lastPlaneState.windSpeed === effectiveWindSpeed
    ) {
      return;
    }

    this.lastPlaneState = {
      lat: latitudeDeg,
      lng: longitudeDeg,
      yaw: effectiveYaw,
      navBearing: effectiveNavBearing,
      windDir: effectiveWindDir,
      windSpeed: effectiveWindSpeed,
    };
    const latLng = L.latLng(latitudeDeg, longitudeDeg);

    if (!this.planeMarker) {
      this.planeMarker = L.marker(latLng, {
        icon: createPlaneIcon(
          effectiveYaw,
          effectiveNavBearing,
          effectiveWindDir,
          effectiveWindSpeed,
        ),
        keyboard: false,
        interactive: false,
        opacity: 0,
        zIndexOffset: 6000,
      }).addTo(this.map);

      const element = this.planeMarker.getElement();
      this.planeIconBody = element?.querySelector('.plane-marker-icon__body') ?? undefined;
      this.planeHeadingLine =
        element?.querySelector('.plane-marker-icon__heading-line') ?? undefined;
      this.planeNavBearingLayer =
        element?.querySelector('.plane-marker-icon__nav-bearing') ?? undefined;
      this.planeWindLayer =
        element?.querySelector('.plane-marker-icon__wind') ?? undefined;
      this.planeWindLine =
        element?.querySelector('.plane-marker-icon__wind-line') ?? undefined;
      this.planeWindArrow =
        element?.querySelector('.plane-marker-icon__wind-arrow') ?? undefined;
      this.applyPlaneRotation(effectiveYaw);
      this.applyNavBearingRotation(effectiveNavBearing);
      this.applyWindRotation(effectiveWindDir);
      this.applyWindScale(effectiveWindSpeed);
      this.applyDisplayLineVisibility();
      this.planeMarker.setOpacity(1);
    } else {
      this.planeMarker.setLatLng(latLng);
      this.applyPlaneRotation(effectiveYaw);
      this.applyNavBearingRotation(effectiveNavBearing);
      this.applyWindRotation(effectiveWindDir);
      this.applyWindScale(effectiveWindSpeed);
      this.applyDisplayLineVisibility();
      if (this.planeMarker.options.opacity === 0) {
        this.planeMarker.setOpacity(1);
      }
    }
  }

  private applyPlaneRotation(yawDeg: number | null): void {
    const body =
      this.planeIconBody ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__body');
    if (!(body instanceof HTMLElement)) {
      return;
    }

    this.planeIconBody = body;

    if (yawDeg === null || !Number.isFinite(yawDeg)) {
      return;
    }

    const rotation = `rotate(${yawDeg}deg)`;
    if (body.style.transform === rotation) {
      return;
    }

    body.style.transform = rotation;
  }

  private applyNavBearingRotation(navBearingDeg: number | null): void {
    const layer =
      this.planeNavBearingLayer ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__nav-bearing');
    if (!(layer instanceof HTMLElement)) {
      return;
    }

    this.planeNavBearingLayer = layer;

    if (navBearingDeg === null || !Number.isFinite(navBearingDeg)) {
      layer.style.visibility = 'hidden';
      return;
    }

    const rotation = `rotate(${navBearingDeg}deg)`;
    if (layer.style.transform !== rotation) {
      layer.style.transform = rotation;
    }

    layer.style.visibility = this.displaySettings.displayTargetPath() ? 'visible' : 'hidden';
  }

  private applyWindRotation(windDirDeg: number | null): void {
    const layer =
      this.planeWindLayer ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__wind');
    if (!(layer instanceof HTMLElement)) {
      return;
    }

    this.planeWindLayer = layer;

    if (windDirDeg === null || !Number.isFinite(windDirDeg)) {
      layer.style.visibility = 'hidden';
      return;
    }

    const rotation = `rotate(${windDirDeg}deg)`;
    if (layer.style.transform !== rotation) {
      layer.style.transform = rotation;
    }

    const speed = this.lastAppliedWindSpeed;
    const hasSpeed = speed !== null && Number.isFinite(speed) && speed > 0;
    layer.style.visibility =
      hasSpeed && this.displaySettings.displayWind() ? 'visible' : 'hidden';
  }

  private applyWindScale(windSpeedMS: number | null): void {
    const line =
      this.planeWindLine ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__wind-line');
    const arrow =
      this.planeWindArrow ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__wind-arrow');
    if (!(line instanceof HTMLElement) || !(arrow instanceof HTMLElement)) {
      return;
    }

    this.planeWindLine = line;
    this.planeWindArrow = arrow;
    applyWindLineGeometry(line, arrow, windSpeedMS);
  }

  private applyDisplayLineVisibility(): void {
    const headingLine =
      this.planeHeadingLine ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__heading-line');
    if (headingLine instanceof HTMLElement) {
      this.planeHeadingLine = headingLine;
      headingLine.style.visibility = this.displaySettings.displayHeading()
        ? 'visible'
        : 'hidden';
    }

    const navLayer =
      this.planeNavBearingLayer ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__nav-bearing');
    if (navLayer instanceof HTMLElement) {
      this.planeNavBearingLayer = navLayer;
      const hasBearing =
        this.lastAppliedNavBearing !== null && Number.isFinite(this.lastAppliedNavBearing);
      navLayer.style.visibility =
        hasBearing && this.displaySettings.displayTargetPath() ? 'visible' : 'hidden';
    }

    const windLayer =
      this.planeWindLayer ??
      this.planeMarker?.getElement()?.querySelector('.plane-marker-icon__wind');
    if (windLayer instanceof HTMLElement) {
      this.planeWindLayer = windLayer;
      const speed = this.lastAppliedWindSpeed;
      const hasWind =
        this.lastAppliedWindDir !== null &&
        Number.isFinite(this.lastAppliedWindDir) &&
        speed !== null &&
        Number.isFinite(speed) &&
        speed > 0;
      windLayer.style.visibility =
        hasWind && this.displaySettings.displayWind() ? 'visible' : 'hidden';
    }
  }

  private applyHomeMarker(
    latitudeDeg: number,
    longitudeDeg: number,
    recenter: boolean,
    flightKey: string | null,
    altitudeM: number | null,
  ): void {
    if (!this.map) {
      return;
    }

    const effectiveAltitude =
      altitudeM !== null && Number.isFinite(altitudeM) ? altitudeM : this.lastHomeAltitudeM;
    if (effectiveAltitude !== null && Number.isFinite(effectiveAltitude)) {
      this.lastHomeAltitudeM = effectiveAltitude;
    }

    if (
      this.homeMarker &&
      this.lastHomeState &&
      this.lastHomeState.lat === latitudeDeg &&
      this.lastHomeState.lng === longitudeDeg &&
      this.lastHomeState.altitudeM === effectiveAltitude &&
      !recenter
    ) {
      return;
    }

    this.lastHomeState = {
      lat: latitudeDeg,
      lng: longitudeDeg,
      altitudeM: effectiveAltitude,
    };
    const latLng = L.latLng(latitudeDeg, longitudeDeg);

    if (!this.homeMarker) {
      this.homeMarker = L.marker(latLng, {
        icon: createHomeMarkerIcon(),
        keyboard: false,
        interactive: true,
        bubblingMouseEvents: false,
        zIndexOffset: 6500,
      }).addTo(this.map);
    } else {
      this.homeMarker.setLatLng(latLng);
      this.homeMarker.setOpacity(1);
      this.homeMarker.setZIndexOffset(6500);
    }

    this.applyHomeAltitudeTooltip(effectiveAltitude);

    if (recenter) {
      this.map.setView(latLng, Math.max(this.map.getZoom(), 15), { animate: false });
      if (flightKey) {
        this.lastRecenterFlightKey = flightKey;
      }
    }
  }

  private applyHomeAltitudeTooltip(altitudeM: number | null): void {
    if (!this.homeMarker) {
      return;
    }

    if (altitudeM === null || !Number.isFinite(altitudeM)) {
      return;
    }

    const text = `Alt: ${formatAltitudeMeters(altitudeM)}`;
    const existing = this.homeMarker.getTooltip();
    if (existing) {
      if (existing.getContent() !== text) {
        this.homeMarker.setTooltipContent(text);
      }
      return;
    }

    this.homeMarker.bindTooltip(text, {
      direction: 'top',
      offset: [0, -14],
      opacity: 0.95,
      sticky: true,
      permanent: false,
      interactive: false,
      className: 'home-marker-tooltip',
    });
  }

  private applyTargetMarker(
    latitudeDeg: number,
    longitudeDeg: number,
    altitudeM: number | null,
  ): void {
    if (!this.map) {
      return;
    }

    const effectiveAltitude =
      altitudeM !== null && Number.isFinite(altitudeM) ? altitudeM : this.lastTargetAltitudeM;
    if (effectiveAltitude !== null && Number.isFinite(effectiveAltitude)) {
      this.lastTargetAltitudeM = effectiveAltitude;
    }

    if (
      this.targetMarker &&
      this.lastTargetState &&
      this.lastTargetState.lat === latitudeDeg &&
      this.lastTargetState.lng === longitudeDeg &&
      this.lastTargetState.altitudeM === effectiveAltitude
    ) {
      return;
    }

    this.lastTargetState = {
      lat: latitudeDeg,
      lng: longitudeDeg,
      altitudeM: effectiveAltitude,
    };
    const latLng = L.latLng(latitudeDeg, longitudeDeg);

    if (!this.targetMarker) {
      this.targetMarker = L.marker(latLng, {
        icon: createTargetMarkerIcon(),
        keyboard: false,
        interactive: true,
        bubblingMouseEvents: false,
        zIndexOffset: 7000,
      }).addTo(this.map);
    } else {
      this.targetMarker.setLatLng(latLng);
      this.targetMarker.setOpacity(1);
      this.targetMarker.setZIndexOffset(7000);
    }

    this.applyTargetAltitudeTooltip(effectiveAltitude);
  }

  private applyTargetAltitudeTooltip(altitudeM: number | null): void {
    if (!this.targetMarker) {
      return;
    }

    if (altitudeM === null || !Number.isFinite(altitudeM)) {
      return;
    }

    const text = `Rel alt: ${formatAltitudeMeters(altitudeM)}`;
    const existing = this.targetMarker.getTooltip();
    if (existing) {
      if (existing.getContent() !== text) {
        this.targetMarker.setTooltipContent(text);
      }
      return;
    }

    this.targetMarker.bindTooltip(text, {
      direction: 'top',
      offset: [0, -14],
      opacity: 0.95,
      sticky: true,
      permanent: false,
      interactive: false,
      className: 'target-marker-tooltip',
    });
  }
}

const PLANE_IMAGE_WIDTH = 40;
const PLANE_IMAGE_HEIGHT = 46;
const PLANE_HEADING_LINE_LENGTH = 100;
/** Base wind line length in px (before speed scaling). */
const WIND_LINE_BASE_PX = 40;
/** Extra line length in px per 1 m/s wind. */
const WIND_LINE_PX_PER_MS = 5;
/** Fixed arrow tip height in px. */
const WIND_ARROW_LENGTH_PX = 9;
/** Half-width of the arrow tip base in px. */
const WIND_ARROW_HALF_WIDTH_PX = 5;
const PLANE_ANCHOR_X = PLANE_IMAGE_WIDTH / 2;
const PLANE_ANCHOR_Y = PLANE_IMAGE_HEIGHT / 2;
const PLANE_PADDING_TOP = PLANE_HEADING_LINE_LENGTH - PLANE_ANCHOR_Y;
const PLANE_ICON_WIDTH = PLANE_IMAGE_WIDTH;
const PLANE_ICON_HEIGHT = PLANE_PADDING_TOP + PLANE_IMAGE_HEIGHT;
const PLANE_ICON_ANCHOR_Y = PLANE_PADDING_TOP + PLANE_ANCHOR_Y;

function formatAltitudeMeters(altitudeM: number): string {
  const rounded = Math.round(altitudeM * 10) / 10;
  return Number.isInteger(rounded) ? `${rounded} m` : `${rounded.toFixed(1)} m`;
}

function windLineLengthPx(windSpeedMS: number | null): number {
  if (windSpeedMS === null || !Number.isFinite(windSpeedMS) || windSpeedMS <= 0) {
    return 0;
  }
  return WIND_LINE_BASE_PX + windSpeedMS * WIND_LINE_PX_PER_MS;
}

function windArrowLengthPx(windSpeedMS: number | null): number {
  if (windSpeedMS === null || !Number.isFinite(windSpeedMS) || windSpeedMS <= 0) {
    return 0;
  }
  return WIND_ARROW_LENGTH_PX;
}

function applyWindLineGeometry(
  line: HTMLElement,
  arrow: HTMLElement,
  windSpeedMS: number | null,
): void {
  const lineLength = windLineLengthPx(windSpeedMS);
  const arrowLength = windArrowLengthPx(windSpeedMS);
  const halfArrow = arrowLength > 0 ? WIND_ARROW_HALF_WIDTH_PX : 0;

  line.style.top = `${PLANE_ICON_ANCHOR_Y - lineLength}px`;
  line.style.height = `${lineLength}px`;
  line.style.visibility = lineLength > 0 ? 'visible' : 'hidden';

  arrow.style.top = `${PLANE_ICON_ANCHOR_Y - lineLength - arrowLength}px`;
  arrow.style.marginLeft = `${-halfArrow}px`;
  arrow.style.borderLeftWidth = `${halfArrow}px`;
  arrow.style.borderRightWidth = `${halfArrow}px`;
  arrow.style.borderBottomWidth = `${arrowLength}px`;
  arrow.style.visibility = arrowLength > 0 ? 'visible' : 'hidden';
}

function createPlaneIcon(
  yawDeg: number | null,
  navBearingDeg: number | null,
  windDirDeg: number | null,
  windSpeedMS: number | null,
): L.DivIcon {
  const root = document.createElement('div');
  root.className = 'plane-marker-icon__root';
  root.style.position = 'relative';
  root.style.width = `${PLANE_ICON_WIDTH}px`;
  root.style.height = `${PLANE_ICON_HEIGHT}px`;
  root.style.overflow = 'visible';

  const transformOrigin = `${PLANE_ANCHOR_X}px ${PLANE_ICON_ANCHOR_Y}px`;

  const windLayer = document.createElement('div');
  windLayer.className = 'plane-marker-icon__wind';
  windLayer.style.position = 'absolute';
  windLayer.style.left = '0';
  windLayer.style.top = '0';
  windLayer.style.width = `${PLANE_ICON_WIDTH}px`;
  windLayer.style.height = `${PLANE_ICON_HEIGHT}px`;
  windLayer.style.transformOrigin = transformOrigin;
  windLayer.style.pointerEvents = 'none';
  windLayer.style.overflow = 'visible';

  const windLine = document.createElement('div');
  windLine.className = 'plane-marker-icon__wind-line';
  windLine.setAttribute('aria-hidden', 'true');
  windLine.style.position = 'absolute';
  windLine.style.left = `${PLANE_ANCHOR_X}px`;
  windLine.style.width = '2px';
  windLine.style.marginLeft = '-1px';
  windLine.style.background = '#006400';
  windLine.style.pointerEvents = 'none';
  windLayer.appendChild(windLine);

  const windArrow = document.createElement('div');
  windArrow.className = 'plane-marker-icon__wind-arrow';
  windArrow.setAttribute('aria-hidden', 'true');
  windArrow.style.position = 'absolute';
  windArrow.style.left = `${PLANE_ANCHOR_X}px`;
  windArrow.style.width = '0';
  windArrow.style.height = '0';
  windArrow.style.borderLeftStyle = 'solid';
  windArrow.style.borderRightStyle = 'solid';
  windArrow.style.borderBottomStyle = 'solid';
  windArrow.style.borderLeftColor = 'transparent';
  windArrow.style.borderRightColor = 'transparent';
  windArrow.style.borderBottomColor = '#006400';
  windArrow.style.pointerEvents = 'none';
  windLayer.appendChild(windArrow);

  applyWindLineGeometry(windLine, windArrow, windSpeedMS);

  if (
    windDirDeg !== null &&
    Number.isFinite(windDirDeg) &&
    windSpeedMS !== null &&
    Number.isFinite(windSpeedMS) &&
    windSpeedMS > 0
  ) {
    windLayer.style.transform = `rotate(${windDirDeg}deg)`;
    windLayer.style.visibility = 'visible';
  } else {
    windLayer.style.visibility = 'hidden';
  }

  const navLayer = document.createElement('div');
  navLayer.className = 'plane-marker-icon__nav-bearing';
  navLayer.style.position = 'absolute';
  navLayer.style.left = '0';
  navLayer.style.top = '0';
  navLayer.style.width = `${PLANE_ICON_WIDTH}px`;
  navLayer.style.height = `${PLANE_ICON_HEIGHT}px`;
  navLayer.style.transformOrigin = transformOrigin;
  navLayer.style.pointerEvents = 'none';

  const navLine = document.createElement('div');
  navLine.className = 'plane-marker-icon__nav-bearing-line';
  navLine.setAttribute('aria-hidden', 'true');
  navLine.style.position = 'absolute';
  navLine.style.left = `${PLANE_ANCHOR_X}px`;
  navLine.style.top = '0';
  navLine.style.width = '1px';
  navLine.style.height = `${PLANE_HEADING_LINE_LENGTH}px`;
  navLine.style.marginLeft = '-0.5px';
  navLine.style.background = '#c2410c';
  navLine.style.pointerEvents = 'none';
  navLayer.appendChild(navLine);

  if (navBearingDeg !== null && Number.isFinite(navBearingDeg)) {
    navLayer.style.transform = `rotate(${navBearingDeg}deg)`;
    navLayer.style.visibility = 'visible';
  } else {
    navLayer.style.visibility = 'hidden';
  }

  const body = document.createElement('div');
  body.className = 'plane-marker-icon__body';
  body.style.position = 'absolute';
  body.style.left = '0';
  body.style.top = '0';
  body.style.width = `${PLANE_ICON_WIDTH}px`;
  body.style.height = `${PLANE_ICON_HEIGHT}px`;
  body.style.transformOrigin = transformOrigin;

  const line = document.createElement('div');
  line.className = 'plane-marker-icon__heading-line';
  line.setAttribute('aria-hidden', 'true');
  line.style.position = 'absolute';
  line.style.left = `${PLANE_ANCHOR_X}px`;
  line.style.top = '0';
  line.style.width = '1px';
  line.style.height = `${PLANE_HEADING_LINE_LENGTH}px`;
  line.style.marginLeft = '-0.5px';
  line.style.background = '#f00';
  line.style.pointerEvents = 'none';

  const image = document.createElement('img');
  image.src = 'assets/icons/drone.png';
  image.alt = '';
  image.draggable = false;
  image.className = 'plane-marker-icon__image';
  image.style.position = 'absolute';
  image.style.left = '0';
  image.style.top = `${PLANE_PADDING_TOP}px`;
  image.style.width = `${PLANE_IMAGE_WIDTH}px`;
  image.style.height = `${PLANE_IMAGE_HEIGHT}px`;
  image.style.display = 'block';

  body.appendChild(line);
  body.appendChild(image);

  if (yawDeg !== null && Number.isFinite(yawDeg)) {
    body.style.transform = `rotate(${yawDeg}deg)`;
  }

  root.appendChild(windLayer);
  root.appendChild(navLayer);
  root.appendChild(body);

  return L.divIcon({
    className: 'plane-marker-icon',
    html: root,
    iconSize: [PLANE_ICON_WIDTH, PLANE_ICON_HEIGHT],
    iconAnchor: [PLANE_ANCHOR_X, PLANE_ICON_ANCHOR_Y],
  });
}

function createHomeMarkerIcon(): L.DivIcon {
  const badge = document.createElement('div');
  badge.className = 'home-marker-icon__badge';
  badge.setAttribute('aria-hidden', 'true');
  badge.textContent = 'H';
  badge.style.display = 'flex';
  badge.style.alignItems = 'center';
  badge.style.justifyContent = 'center';
  badge.style.width = '1.75rem';
  badge.style.height = '1.75rem';
  badge.style.border = '2px solid #fff';
  badge.style.borderRadius = '50%';
  badge.style.background = '#5d05f5';
  badge.style.boxShadow = '0 2px 6px rgb(0 0 0 / 35%)';
  badge.style.color = '#fff';
  badge.style.fontSize = '0.875rem';
  badge.style.fontWeight = '800';
  badge.style.lineHeight = '1';

  return L.divIcon({
    className: 'leaflet-div-icon home-marker-icon',
    html: badge,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
  });
}

function createTargetMarkerIcon(): L.DivIcon {
  const badge = document.createElement('div');
  badge.className = 'target-marker-icon__badge';
  badge.setAttribute('aria-hidden', 'true');
  badge.textContent = 'T';
  badge.style.display = 'flex';
  badge.style.alignItems = 'center';
  badge.style.justifyContent = 'center';
  badge.style.width = '1.5rem';
  badge.style.height = '1.5rem';
  badge.style.border = '2px solid #fff';
  badge.style.borderRadius = '50%';
  badge.style.background = '#e11d48';
  badge.style.boxShadow = '0 2px 6px rgb(0 0 0 / 35%)';
  badge.style.color = '#fff';
  badge.style.fontSize = '0.75rem';
  badge.style.fontWeight = '800';
  badge.style.lineHeight = '1';

  return L.divIcon({
    className: 'leaflet-div-icon target-marker-icon',
    html: badge,
    iconSize: [24, 24],
    iconAnchor: [12, 12],
  });
}
