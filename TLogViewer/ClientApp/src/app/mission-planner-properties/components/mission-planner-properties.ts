import { Component, computed, effect, input, signal } from '@angular/core';
import { ModalContentHostDirective } from '../../shared/modal/directives/modal-content-host.directive';
import { ModalContentBase } from '../../shared/modal/models/modal-content.model';
import {
  MISSION_PLANNER_PROPERTIES,
  MissionPlannerProperty,
  MissionPlannerPropertyKey,
} from '../models/mission-planner-properties.const';

interface MissionPlannerPropertyGroup {
  letter: string;
  properties: MissionPlannerProperty[];
}

@Component({
  selector: 'app-mission-planner-properties',
  standalone: true,
  hostDirectives: [ModalContentHostDirective],
  providers: [{ provide: ModalContentBase, useExisting: MissionPlannerPropertiesComponent }],
  templateUrl: './mission-planner-properties.html',
  styleUrl: './mission-planner-properties.scss',
})
export class MissionPlannerPropertiesComponent extends ModalContentBase<MissionPlannerProperty[]> {
  readonly selectedKeys = input<readonly MissionPlannerPropertyKey[]>([]);

  protected readonly searchQuery = signal('');
  /** Keys in selection order. */
  private readonly selectedOrder = signal<MissionPlannerPropertyKey[]>([]);

  protected readonly groupedProperties = computed(() => {
    const sorted = [...this.filteredProperties()].sort((a, b) =>
      this.getSortKey(a).localeCompare(this.getSortKey(b), undefined, { sensitivity: 'base' }),
    );

    const groups: MissionPlannerPropertyGroup[] = [];
    for (const property of sorted) {
      const letter = this.getGroupLetter(property);
      const lastGroup = groups.at(-1);
      if (lastGroup?.letter === letter) {
        lastGroup.properties.push(property);
      } else {
        groups.push({ letter, properties: [property] });
      }
    }

    return groups;
  });

  private readonly filteredProperties = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    if (!query) {
      return MISSION_PLANNER_PROPERTIES;
    }
    return MISSION_PLANNER_PROPERTIES.filter((property) => this.matchesSearch(property, query));
  });

  constructor() {
    super();
    effect(() => {
      this.selectedOrder.set([...this.selectedKeys()]);
    });
  }

  override getModalValue(): MissionPlannerProperty[] {
    return this.selectedOrder().map((key, order) => {
      const catalog = MISSION_PLANNER_PROPERTIES.find((property) => property.key === key);
      return {
        key,
        label: catalog?.label,
        order,
      };
    });
  }

  protected isSelected(key: MissionPlannerPropertyKey): boolean {
    return this.selectedOrder().includes(key);
  }

  protected toggleProperty(key: MissionPlannerPropertyKey, checked: boolean): void {
    this.selectedOrder.update((current) => {
      if (checked) {
        if (current.includes(key)) {
          return current;
        }
        return [...current, key];
      }
      return current.filter((selectedKey) => selectedKey !== key);
    });
  }

  protected onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  protected clearSearch(): void {
    this.searchQuery.set('');
  }

  private getSortKey(property: MissionPlannerProperty): string {
    return property.label ?? property.key;
  }

  private getGroupLetter(property: MissionPlannerProperty): string {
    const firstChar = this.getSortKey(property).charAt(0).toUpperCase();
    return /[A-Z]/.test(firstChar) ? firstChar : '#';
  }

  private matchesSearch(property: MissionPlannerProperty, query: string): boolean {
    const formatted = property.label ? `${property.label} (${property.key})` : property.key;
    return (
      property.key.toLowerCase().includes(query) ||
      (property.label?.toLowerCase().includes(query) ?? false) ||
      formatted.toLowerCase().includes(query)
    );
  }
}
