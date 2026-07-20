import { Directive, inject, OnDestroy, OnInit } from '@angular/core';
import { ModalContentBase } from '../models/modal-content.model';
import { ModalContentRegistry } from '../services/modal-content-registry.service';

/**
 * Registers the host component with the parent modal.
 * Add via `hostDirectives: [ModalContentHostDirective]` on inner components
 * that extend {@link ModalContentBase}.
 */
@Directive({
  selector: '[appModalContentHost]',
})
export class ModalContentHostDirective implements OnInit, OnDestroy {
  private readonly registry = inject(ModalContentRegistry);
  private readonly host = inject(ModalContentBase, { self: true });

  ngOnInit(): void {
    this.registry.register(this.host);
  }

  ngOnDestroy(): void {
    this.registry.unregister(this.host);
  }
}
