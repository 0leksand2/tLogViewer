import { Component, effect, input, signal } from '@angular/core';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';
import { DEFAULT_GPS_SOURCE, type MapGpsSource } from '../models/map-gps-source';
import {
  GPS_SOURCE_OPTIONS,
  MapDisplaySettings,
  TRAIL_LENGTH_OPTIONS,
} from '../services/map-display-settings.service';

@Component({
  selector: 'app-map-settings-menu',
  standalone: true,
  hostDirectives: [ModalContentHostDirective],
  providers: [{ provide: ModalContentBase, useExisting: MapSettingsMenuComponent }],
  templateUrl: './map-settings-menu.html',
  styleUrl: './map-settings-menu.scss',
})
export class MapSettingsMenuComponent extends ModalContentBase<MapDisplaySettings> {
  readonly displayHeading = input(true);
  readonly displayTargetPath = input(true);
  readonly displayWind = input(true);
  readonly displayTrail = input(true);
  readonly displayFullTrail = input(false);
  readonly trailLengthSeconds = input(60);
  readonly gpsSource = input<MapGpsSource>(DEFAULT_GPS_SOURCE);

  protected readonly trailLengthOptions = TRAIL_LENGTH_OPTIONS;
  protected readonly gpsSourceOptions = GPS_SOURCE_OPTIONS;

  protected readonly draftHeading = signal(true);
  protected readonly draftTargetPath = signal(true);
  protected readonly draftWind = signal(true);
  protected readonly draftTrail = signal(true);
  protected readonly draftFullTrail = signal(false);
  protected readonly draftTrailLengthSeconds = signal(60);
  protected readonly draftGpsSource = signal<MapGpsSource>(DEFAULT_GPS_SOURCE);

  constructor() {
    super();
    effect(() => {
      this.draftHeading.set(this.displayHeading());
      this.draftTargetPath.set(this.displayTargetPath());
      this.draftWind.set(this.displayWind());
      this.draftTrail.set(this.displayTrail());
      this.draftFullTrail.set(this.displayFullTrail());
      this.draftTrailLengthSeconds.set(this.trailLengthSeconds());
      this.draftGpsSource.set(this.gpsSource());
    });
  }

  override getModalValue(): MapDisplaySettings {
    return {
      displayHeading: this.draftHeading(),
      displayTargetPath: this.draftTargetPath(),
      displayWind: this.draftWind(),
      displayTrail: this.draftTrail(),
      displayFullTrail: this.draftFullTrail(),
      trailLengthSeconds: Number(this.draftTrailLengthSeconds()),
      gpsSource: this.draftGpsSource(),
    };
  }

  protected onDisplayHeadingChange(checked: boolean): void {
    this.draftHeading.set(checked);
  }

  protected onDisplayTargetPathChange(checked: boolean): void {
    this.draftTargetPath.set(checked);
  }

  protected onDisplayWindChange(checked: boolean): void {
    this.draftWind.set(checked);
  }

  protected onDisplayTrailChange(checked: boolean): void {
    this.draftTrail.set(checked);
  }

  protected onDisplayFullTrailChange(checked: boolean): void {
    this.draftFullTrail.set(checked);
  }

  protected onTrailLengthChange(value: string | number): void {
    const parsed = typeof value === 'number' ? value : Number(value);
    if (!Number.isFinite(parsed)) {
      return;
    }
    this.draftTrailLengthSeconds.set(parsed);
  }

  protected onGpsSourceChange(value: string): void {
    const match = GPS_SOURCE_OPTIONS.find((option) => option.value === value);
    if (match) {
      this.draftGpsSource.set(match.value);
    }
  }
}
