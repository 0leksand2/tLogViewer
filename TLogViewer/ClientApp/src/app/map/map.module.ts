import { NgModule } from '@angular/core';
import { MapComponent } from './components/map';
import { MapSettingsMenuComponent } from './components/map-settings-menu';

@NgModule({
  imports: [MapComponent, MapSettingsMenuComponent],
  exports: [MapComponent, MapSettingsMenuComponent],
})
export class MapModule {}
