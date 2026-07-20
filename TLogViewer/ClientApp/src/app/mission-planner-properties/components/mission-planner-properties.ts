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
  private readonly selected = signal<ReadonlySet<MissionPlannerPropertyKey>>(new Set());

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
      this.selected.set(new Set(this.selectedKeys()));
    });
  }

  override getModalValue(): MissionPlannerProperty[] {
    const selected = this.selected();
    return MISSION_PLANNER_PROPERTIES.filter((property) => selected.has(property.key));
  }

  protected isSelected(key: MissionPlannerPropertyKey): boolean {
    return this.selected().has(key);
  }

  protected toggleProperty(key: MissionPlannerPropertyKey, checked: boolean): void {
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(key);
      } else {
        next.delete(key);
      }
      return next;
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
