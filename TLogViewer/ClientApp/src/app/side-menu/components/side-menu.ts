import { Component, input, model, output, signal } from '@angular/core';
import { TabsModule } from '../../shared/tabs/tabs.module';
import { TabItem } from '../../shared/tabs/models/tab-item.model';
import { MissionPlannerProperty } from '../../mission-planner-properties/models/mission-planner-properties.const';
import { FlightStatusText } from '../../tlog-load-menu/models/mav-message.models';
import { SideMenuDataTabComponent } from './side-menu-data-tab/side-menu-data-tab';
import { SideMenuMessagesTabComponent } from './side-menu-messages-tab/side-menu-messages-tab';

@Component({
  selector: 'app-side-menu',
  standalone: true,
  imports: [TabsModule, SideMenuDataTabComponent, SideMenuMessagesTabComponent],
  templateUrl: './side-menu.html',
  styleUrl: './side-menu.scss',
})
export class SideMenuComponent {
  readonly open = model(true);
  readonly selectedProperties = input<readonly MissionPlannerProperty[]>([]);
  readonly statusTexts = input<Record<string, FlightStatusText[]> | null>(null);
  readonly playing = input(false);
  readonly openProperties = output<void>();
  readonly propertiesReordered = output<MissionPlannerProperty[]>();

  protected readonly tabs: readonly TabItem[] = [
    { id: 'data', label: 'Data' },
    { id: 'messages', label: 'Messages' },
  ];

  protected readonly activeTabId = signal('data');

  toggle(): void {
    this.open.update((value) => !value);
  }

  close(): void {
    this.open.set(false);
  }

  requestProperties(): void {
    this.openProperties.emit();
  }

  onPropertiesReordered(properties: MissionPlannerProperty[]): void {
    this.propertiesReordered.emit(properties);
  }
}
