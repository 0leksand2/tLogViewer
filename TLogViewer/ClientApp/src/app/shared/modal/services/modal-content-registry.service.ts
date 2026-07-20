import { Injectable } from '@angular/core';
import { ModalContent } from '../models/modal-content.model';
@Injectable()
export class ModalContentRegistry {
  private content?: ModalContent<unknown>;

  register(content: ModalContent<unknown>): void {
    this.content = content;
  }

  unregister(content: ModalContent<unknown>): void {
    if (this.content === content) {
      this.content = undefined;
    }
  }

  getValue<T = unknown>(): T | undefined {
    return this.content?.getModalValue() as T | undefined;
  }

  isValid(): boolean {
    return this.content?.isModalValid?.() ?? true;
  }
}
