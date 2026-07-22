import { NgModule } from '@angular/core';
import { MapComponent } from './components/map';
import { MapSettingsMenuComponent } from './components/map-settings-menu';
import { MapDisplayHelpComponent } from './components/map-display-help';

@NgModule({
  imports: [MapComponent, MapSettingsMenuComponent, MapDisplayHelpComponent],
  exports: [MapComponent, MapSettingsMenuComponent, MapDisplayHelpComponent],
})
export class MapModule {}
