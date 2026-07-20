import { Component, computed, input, output } from '@angular/core';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MissionPlannerProperty } from '../../../mission-planner-properties/models/mission-planner-properties.const';

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
  readonly properties = input<readonly MissionPlannerProperty[]>([]);
  readonly values = input<ReadonlyMap<string, string | number | null | undefined>>(new Map());
  readonly openProperties = output<void>();
  readonly propertiesReordered = output<MissionPlannerProperty[]>();

  protected readonly tiles = computed<TelemetryTile[]>(() => {
    const values = this.values();
    return [...this.properties()]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((property) => ({
        key: property.key,
        name: property.label ?? property.key,
        value: this.formatValue(values.get(property.key)),
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

  private formatValue(value: string | number | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '0.0';
    }
    if (typeof value === 'number') {
      return Number.isFinite(value) ? value.toFixed(1) : '0.0';
    }
    return value;
  }
}
