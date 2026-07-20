import { Component, computed, effect, input, signal } from '@angular/core';

const FIGHTER_JET_ICON = 'assets/icons/fighter-jet.png';
const ZLOMSKY_ICON = 'assets/icons/zlomsky.png';
const DEFAULT_ICON_SIZE_PX = 128;
const ZLOMSKY_ICON_SIZE_PX = DEFAULT_ICON_SIZE_PX * 5;

@Component({
  selector: 'app-spinner',
  standalone: true,
  templateUrl: './spinner.html',
  styleUrl: './spinner.scss',
})
export class SpinnerComponent {
  readonly visible = input(false);
  readonly message = input('Loading…');

  protected readonly iconSrc = signal(FIGHTER_JET_ICON);

  protected readonly iconSizePx = computed(() =>
    this.iconSrc() === ZLOMSKY_ICON ? ZLOMSKY_ICON_SIZE_PX : DEFAULT_ICON_SIZE_PX,
  );

  constructor() {
    effect(() => {
      if (!this.visible()) {
        return;
      }

      this.iconSrc.set(Math.random() < 0.1 ? ZLOMSKY_ICON : FIGHTER_JET_ICON);
    });
  }
}
