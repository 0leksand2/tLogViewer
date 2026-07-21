import {
  afterNextRender,
  Component,
  ElementRef,
  OnDestroy,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';

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
  private map?: L.Map;
  private homeMarker?: L.Marker;
  private planeMarker?: L.Marker;
  private pendingHome: {
    lat: number;
    lng: number;
    recenter: boolean;
    flightKey: string | null;
  } | null = null;
  private pendingPlane: { lat: number; lng: number; yaw: number | null } | null = null;
  private lastRecenterFlightKey: string | null = null;
  private lastPlaneState: { lat: number; lng: number; yaw: number | null } | null = null;
  private lastAppliedYaw: number | null = null;
  private lastPlaneFlightKey: string | null = null;
  private planeIconBody?: HTMLElement;

  constructor() {
    afterNextRender(() => this.initMap());
  }

  ngOnDestroy(): void {
    this.homeMarker = undefined;
    this.planeMarker = undefined;
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
  ): void {
    if (
      latitudeDeg === null ||
      longitudeDeg === null ||
      !Number.isFinite(latitudeDeg) ||
      !Number.isFinite(longitudeDeg)
    ) {
      this.pendingHome = null;
      this.lastRecenterFlightKey = null;
      this.homeMarker?.setOpacity(0);
      return;
    }

    const recenter =
      options.recenter === true &&
      flightKey !== null &&
      flightKey !== this.lastRecenterFlightKey;

    if (!this.map) {
      this.pendingHome = { lat: latitudeDeg, lng: longitudeDeg, recenter, flightKey };
      return;
    }

    this.applyHomeMarker(latitudeDeg, longitudeDeg, recenter, flightKey);
  }

  /** Show or hide the drone at the current playback position. Pass null to clear. */
  setPlaneLocation(
    latitudeDeg: number | null,
    longitudeDeg: number | null,
    yawDeg: number | null = null,
    flightKey: string | null = null,
  ): void {
    if (
      latitudeDeg === null ||
      longitudeDeg === null ||
      !Number.isFinite(latitudeDeg) ||
      !Number.isFinite(longitudeDeg)
    ) {
      this.pendingPlane = null;
      this.resetPlaneMarkerState();
      return;
    }

    if (flightKey !== this.lastPlaneFlightKey) {
      this.resetPlaneMarkerState({ keepFlightKey: true });
      this.lastPlaneFlightKey = flightKey;
    }

    if (!this.map) {
      this.pendingPlane = { lat: latitudeDeg, lng: longitudeDeg, yaw: yawDeg };
      return;
    }

    this.applyPlaneMarker(latitudeDeg, longitudeDeg, yawDeg);
  }

  private resetPlaneMarkerState(options: { keepFlightKey?: boolean } = {}): void {
    if (!options.keepFlightKey) {
      this.lastPlaneFlightKey = null;
    }
    this.lastPlaneState = null;
    this.lastAppliedYaw = null;
    this.planeIconBody = undefined;
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
      );
    }

    if (this.pendingPlane) {
      const pending = this.pendingPlane;
      this.pendingPlane = null;
      this.applyPlaneMarker(pending.lat, pending.lng, pending.yaw);
    }

    requestAnimationFrame(() => this.map?.invalidateSize());
  }

  private applyPlaneMarker(
    latitudeDeg: number,
    longitudeDeg: number,
    yawDeg: number | null,
  ): void {
    if (!this.map) {
      return;
    }

    const effectiveYaw = yawDeg ?? this.lastAppliedYaw;
    if (effectiveYaw !== null && Number.isFinite(effectiveYaw)) {
      this.lastAppliedYaw = effectiveYaw;
    }

    if (
      this.planeMarker &&
      this.lastPlaneState &&
      this.lastPlaneState.lat === latitudeDeg &&
      this.lastPlaneState.lng === longitudeDeg &&
      this.lastPlaneState.yaw === effectiveYaw
    ) {
      return;
    }

    this.lastPlaneState = { lat: latitudeDeg, lng: longitudeDeg, yaw: effectiveYaw };
    const latLng = L.latLng(latitudeDeg, longitudeDeg);

    if (!this.planeMarker) {
      this.planeMarker = L.marker(latLng, {
        icon: createPlaneIcon(effectiveYaw),
        keyboard: false,
        interactive: false,
        opacity: 0,
        zIndexOffset: 6000,
      }).addTo(this.map);

      this.planeIconBody =
        this.planeMarker.getElement()?.querySelector('.plane-marker-icon__body') ?? undefined;
      this.applyPlaneRotation(effectiveYaw);
      this.planeMarker.setOpacity(1);
    } else {
      this.planeMarker.setLatLng(latLng);
      this.applyPlaneRotation(effectiveYaw);
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

  private applyHomeMarker(
    latitudeDeg: number,
    longitudeDeg: number,
    recenter: boolean,
    flightKey: string | null,
  ): void {
    if (!this.map) {
      return;
    }

    const latLng = L.latLng(latitudeDeg, longitudeDeg);

    if (!this.homeMarker) {
      this.homeMarker = L.marker(latLng, {
        icon: createHomeMarkerIcon(),
        keyboard: false,
        interactive: false,
        zIndexOffset: 5000,
      }).addTo(this.map);
    } else {
      this.homeMarker.setLatLng(latLng);
      this.homeMarker.setOpacity(1);
      this.homeMarker.setZIndexOffset(5000);
    }

    if (recenter) {
      this.map.setView(latLng, Math.max(this.map.getZoom(), 15), { animate: false });
      if (flightKey) {
        this.lastRecenterFlightKey = flightKey;
      }
    }
  }
}

const PLANE_IMAGE_WIDTH = 40;
const PLANE_IMAGE_HEIGHT = 46;
const PLANE_HEADING_LINE_LENGTH = 100;
const PLANE_ANCHOR_X = PLANE_IMAGE_WIDTH / 2;
const PLANE_ANCHOR_Y = PLANE_IMAGE_HEIGHT / 2;
const PLANE_PADDING_TOP = PLANE_HEADING_LINE_LENGTH - PLANE_ANCHOR_Y;
const PLANE_ICON_WIDTH = PLANE_IMAGE_WIDTH;
const PLANE_ICON_HEIGHT = PLANE_PADDING_TOP + PLANE_IMAGE_HEIGHT;
const PLANE_ICON_ANCHOR_Y = PLANE_PADDING_TOP + PLANE_ANCHOR_Y;

function createPlaneIcon(yawDeg: number | null): L.DivIcon {
  const body = document.createElement('div');
  body.className = 'plane-marker-icon__body';
  body.style.width = `${PLANE_ICON_WIDTH}px`;
  body.style.height = `${PLANE_ICON_HEIGHT}px`;
  body.style.transformOrigin = `${PLANE_ANCHOR_X}px ${PLANE_ICON_ANCHOR_Y}px`;

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

  return L.divIcon({
    className: 'plane-marker-icon',
    html: body,
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
    className: 'home-marker-icon',
    html: badge,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
  });
}
