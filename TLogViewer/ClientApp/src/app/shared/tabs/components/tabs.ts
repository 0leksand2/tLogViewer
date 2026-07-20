import { Component, input, model } from '@angular/core';
import { TabItem } from '../models/tab-item.model';

@Component({
  selector: 'app-tabs',
  standalone: true,
  templateUrl: './tabs.html',
  styleUrl: './tabs.scss',
})
export class TabsComponent {
  readonly tabs = input.required<readonly TabItem[]>();
  readonly activeTabId = model.required<string>();
  readonly ariaLabel = input('Tabs');

  protected selectTab(tabId: string): void {
    this.activeTabId.set(tabId);
  }
}
