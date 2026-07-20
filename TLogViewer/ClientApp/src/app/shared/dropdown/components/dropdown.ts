import {
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  input,
  model,
  signal,
} from '@angular/core';
import { DropdownOption } from '../models/dropdown-option.model';

@Component({
  selector: 'app-dropdown',
  standalone: true,
  templateUrl: './dropdown.html',
  styleUrl: './dropdown.scss',
})
export class DropdownComponent {
  readonly options = input.required<DropdownOption[]>();
  readonly placeholder = input('Select…');
  readonly ariaLabel = input('Select option');
  readonly disabled = input(false);
  readonly value = model<string | null>(null);

  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly open = signal(false);

  protected readonly selectedLabel = computed(() => {
    const current = this.value();
    if (current === null) {
      return null;
    }
    return this.options().find((option) => option.value === current)?.label ?? null;
  });

  toggle(): void {
    if (this.disabled()) {
      return;
    }
    this.open.update((isOpen) => !isOpen);
  }

  selectOption(option: DropdownOption): void {
    this.value.set(option.value);
    this.open.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) {
      return;
    }
    if (this.host.nativeElement.contains(event.target as Node)) {
      return;
    }
    this.open.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.open.set(false);
  }
}
