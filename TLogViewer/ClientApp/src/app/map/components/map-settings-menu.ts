import { Component, effect, input, signal } from '@angular/core';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';
import { MapDisplaySettings } from '../services/map-display-settings.service';

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

  protected readonly draftHeading = signal(true);
  protected readonly draftTargetPath = signal(true);
  protected readonly draftWind = signal(true);

  constructor() {
    super();
    effect(() => {
      this.draftHeading.set(this.displayHeading());
      this.draftTargetPath.set(this.displayTargetPath());
      this.draftWind.set(this.displayWind());
    });
  }

  override getModalValue(): MapDisplaySettings {
    return {
      displayHeading: this.draftHeading(),
      displayTargetPath: this.draftTargetPath(),
      displayWind: this.draftWind(),
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
}
