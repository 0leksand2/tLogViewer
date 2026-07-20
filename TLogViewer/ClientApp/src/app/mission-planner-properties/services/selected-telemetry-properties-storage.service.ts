import { Injectable } from '@angular/core';
import {
  MISSION_PLANNER_PROPERTIES,
  MissionPlannerProperty,
  MissionPlannerPropertyKey,
} from '../models/mission-planner-properties.const';

const STORAGE_KEY = 'tlog-viewer.selected-telemetry-properties';

@Injectable({ providedIn: 'root' })
export class SelectedTelemetryPropertiesStorage {
  load(): MissionPlannerProperty[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw) as unknown;
      if (!Array.isArray(parsed)) {
        return [];
      }

      const catalogKeys = new Set(MISSION_PLANNER_PROPERTIES.map((property) => property.key));
      const restored: MissionPlannerProperty[] = [];

      for (const [index, item] of parsed.entries()) {
        if (!item || typeof item !== 'object') {
          continue;
        }

        const record = item as Record<string, unknown>;
        const key = typeof record['key'] === 'string' ? (record['key'] as MissionPlannerPropertyKey) : null;
        if (!key || !catalogKeys.has(key)) {
          continue;
        }

        const catalog = MISSION_PLANNER_PROPERTIES.find((property) => property.key === key);
        const order =
          typeof record['order'] === 'number' && Number.isFinite(record['order'])
            ? (record['order'] as number)
            : index;

        restored.push({
          key,
          label: catalog?.label ?? (typeof record['label'] === 'string' ? record['label'] : undefined),
          order,
        });
      }

      return restored.sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
    } catch {
      return [];
    }
  }

  save(properties: readonly MissionPlannerProperty[]): void {
    const payload = properties
      .map((property, index) => ({
        key: property.key,
        label: property.label,
        order: property.order ?? index,
      }))
      .sort((a, b) => a.order - b.order);

    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
  }
}
