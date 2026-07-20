import { Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  templateUrl: './spinner.html',
  styleUrl: './spinner.scss',
})
export class SpinnerComponent {
  readonly visible = input(false);
  readonly message = input('Loading…');
}
