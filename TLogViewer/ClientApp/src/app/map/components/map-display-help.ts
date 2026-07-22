import { Component } from '@angular/core';
import { FLIGHT_MODE_LEGEND } from '../../core/flight-mode';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';

@Component({
  selector: 'app-map-display-help',
  standalone: true,
  hostDirectives: [ModalContentHostDirective],
  providers: [{ provide: ModalContentBase, useExisting: MapDisplayHelpComponent }],
  templateUrl: './map-display-help.html',
  styleUrl: './map-display-help.scss',
})
export class MapDisplayHelpComponent extends ModalContentBase<null> {
  protected readonly modeLegend = FLIGHT_MODE_LEGEND;

  override getModalValue(): null {
    return null;
  }
}
