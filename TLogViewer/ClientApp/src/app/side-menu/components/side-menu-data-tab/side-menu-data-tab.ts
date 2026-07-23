import { Component, computed, inject, input, output } from '@angular/core';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MissionPlannerProperty } from '../../../mission-planner-properties/models/mission-planner-properties.const';
import { CurrentValue } from '../../../core/services/current.value';
import { VfrHudComponent } from '../vfr-hud/vfr-hud';
import { HeadingCompassComponent } from '../heading-compass/heading-compass';

export interface TelemetryTile {
  key: string;
  name: string;
  value: string;
  property: MissionPlannerProperty;
}

@Component({
  selector: 'app-side-menu-data-tab',
  standalone: true,
  imports: [DragDropModule, VfrHudComponent, HeadingCompassComponent],
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

  protected isLastColumn(index: number): boolean {
    return (index + 1) % this.columnCount() === 0;
  }

  protected isLastRow(index: number): boolean {
    const cols = this.columnCount();
    const count = this.tiles().length;
    const remainder = count % cols;
    const lastRowStart = count - (remainder === 0 ? cols : remainder);
    return index >= lastRowStart;
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
      if (!Number.isFinite(value)) {
        return '0.0';
      }
      const display = this.normalizeHeadingDegrees(value, key);
      return display.toFixed(1);
    }
    if (typeof value === 'boolean') {
      return value ? 'true' : 'false';
    }
    if (typeof value === 'string') {
      if (key === 'yaw' || key === 'wind_dir') {
        const parsed = Number(value);
        if (Number.isFinite(parsed)) {
          return this.normalizeHeadingDegrees(parsed, key).toFixed(1);
        }
      }
      return value;
    }
    return String(value);
  }

  /** Negative headings → 0–360° (e.g. -10 → 350). */
  private normalizeHeadingDegrees(degrees: number, key: string): number {
    if (key !== 'yaw' && key !== 'wind_dir') {
      return degrees;
    }
    if (degrees < 0) {
      return 360 + degrees;
    }
    return degrees;
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
