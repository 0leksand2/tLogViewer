import { NgModule } from '@angular/core';
import { ModalComponent } from './components/modal';
import { ModalContentHostDirective } from './directives/modal-content-host.directive';

@NgModule({
  imports: [ModalComponent, ModalContentHostDirective],
  exports: [ModalComponent, ModalContentHostDirective],
})
export class ModalModule {}
