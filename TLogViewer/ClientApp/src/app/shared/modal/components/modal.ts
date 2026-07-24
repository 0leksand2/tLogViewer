import { Component, inject, input, model, output } from '@angular/core';
import { ModalCloseResult } from '../models/modal-content.model';
import { ModalContentRegistry } from '../services/modal-content-registry.service';

@Component({
  selector: 'app-modal',
  standalone: true,
  providers: [ModalContentRegistry],
  templateUrl: './modal.html',
  styleUrl: './modal.scss',
})
export class ModalComponent {
  readonly title = input('');
  readonly open = model(false);
  readonly saveLabel = input('Save');
  readonly cancelLabel = input('Cancel');
  readonly disableSaveWhenInvalid = input(true);
  readonly showCancel = input(true);
  readonly size = input<'default' | 'wide' | 'xl'>('default');

  readonly saved = output<ModalCloseResult>();
  readonly cancelled = output<ModalCloseResult>();

  private readonly contentRegistry = inject(ModalContentRegistry);

  close(): void {
    this.open.set(false);
  }

  onBackdropClick(): void {
    this.emitCancel();
  }

  onSave(): void {
    if (this.disableSaveWhenInvalid() && !this.contentRegistry.isValid()) {
      return;
    }

    this.saved.emit({
      action: 'save',
      value: this.contentRegistry.getValue(),
    });
    this.close();
  }

  onCancel(): void {
    this.emitCancel();
  }

  private emitCancel(): void {
    this.cancelled.emit({
      action: 'cancel',
      value: this.contentRegistry.getValue(),
    });
    this.close();
  }
}
