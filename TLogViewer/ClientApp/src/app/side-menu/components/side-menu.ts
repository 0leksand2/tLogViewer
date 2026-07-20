import { Component, model, output } from '@angular/core';

@Component({
  selector: 'app-side-menu',
  standalone: true,
  templateUrl: './side-menu.html',
  styleUrl: './side-menu.scss',
})
export class SideMenuComponent {
  readonly open = model(true);
  readonly openProperties = output<void>();

  toggle(): void {
    this.open.update((value) => !value);
  }

  close(): void {
    this.open.set(false);
  }

  requestProperties(): void {
    this.openProperties.emit();
  }
}
