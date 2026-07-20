export interface ModalContent<T = unknown> {
  getModalValue(): T;
  isModalValid?(): boolean;
}

export abstract class ModalContentBase<T = unknown> implements ModalContent<T> {
  abstract getModalValue(): T;

  isModalValid(): boolean {
    return true;
  }
}

export interface ModalCloseResult<T = unknown> {
  action: 'save' | 'cancel';
  value: T | undefined;
}
