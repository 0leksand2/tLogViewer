import { Component, computed, inject, input, output } from '@angular/core';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MissionPlannerProperty } from '../../../mission-planner-properties/models/mission-planner-properties.const';
import { CurrentValue } from '../../../core/services/current.value';

export interface TelemetryTile {
  key: string;
  name: string;
  value: string;
  property: MissionPlannerProperty;
}

@Component({
  selector: 'app-side-menu-data-tab',
  standalone: true,
  imports: [DragDropModule],
  templateUrl: './side-menu-data-tab.html',
  styleUrl: './side-menu-data-tab.scss',
})
export class SideMenuDataTabComponent {
  private readonly currentValue = inject(CurrentValue);

  readonly properties = input<readonly MissionPlannerProperty[]>([]);
  readonly openProperties = output<void>();
  readonly propertiesReordered = output<MissionPlannerProperty[]>();

  protected readonly tiles = computed<TelemetryTile[]>(() => {
    const values = this.currentValue.values();
    return [...this.properties()]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((property) => ({
        key: property.key,
        name: property.label ?? property.key,
        value: this.formatValue(values[property.propertyValue], property.key),
        property,
      }));
  });

  /** Columns scale with how many rows the tiles would form. Max 3 per row. */
  protected readonly columnCount = computed(() => {
    const count = this.tiles().length;
    if (count === 0) {
      return 2;
    }

    const rowsAtTwo = Math.ceil(count / 2);
    if (rowsAtTwo <= 3) {
      return 2;
    }

    return 3;
  });

  protected requestProperties(): void {
    this.openProperties.emit();
  }

  protected onTileDropped(event: CdkDragDrop<unknown>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    const reordered = this.tiles().map((tile) => ({ ...tile.property }));
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);

    const withOrder = reordered.map((property, order) => ({
      ...property,
      order,
    }));

    this.propertiesReordered.emit(withOrder);
  }

  private formatValue(value: unknown, key: string): string {
    if (key === 'timeInAirMinSec') {
      return this.formatMinSec(value);
    }
    if (value === null || value === undefined || value === '') {
      return '0.0';
    }
    if (typeof value === 'number') {
      return Number.isFinite(value) ? value.toFixed(1) : '0.0';
    }
    if (typeof value === 'boolean') {
      return value ? 'true' : 'false';
    }
    if (typeof value === 'string') {
      return value;
    }
    return String(value);
  }

  /** Seconds → `m:ss` (e.g. 125.4 → `2:05`). */
  private formatMinSec(value: unknown): string {
    const seconds = typeof value === 'number' ? value : Number(value);
    if (!Number.isFinite(seconds) || seconds <= 0) {
      return '0:00';
    }

    const total = Math.floor(seconds);
    const minutes = Math.floor(total / 60);
    const secs = total % 60;
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  }
}
