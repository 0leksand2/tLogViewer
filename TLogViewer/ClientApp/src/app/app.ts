import { Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { MapComponent } from './map/components/map';
import { MapModule } from './map/map.module';
import { SideMenuModule } from './side-menu/side-menu.module';
import { DropdownModule } from './shared/dropdown/dropdown.module';
import { ModalModule } from './shared/modal/modal.module';
import { DropdownOption } from './shared/dropdown/models/dropdown-option.model';
import { MissionPlannerPropertiesModule } from './mission-planner-properties/mission-planner-properties.module';
import { TlogLoadMenuModule } from './tlog-load-menu/tlog-load-menu.module';
import { FlightSummary, TlogUploadResult } from './tlog-load-menu/models/mav-message.models';
import { ModalCloseResult } from './shared/modal/models/modal-content.model';
import {
  MissionPlannerProperty,
  MissionPlannerPropertyKey,
} from './mission-planner-properties/models/mission-planner-properties.const';
import { SelectedTelemetryPropertiesStorage } from './mission-planner-properties/services/selected-telemetry-properties-storage.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    MapModule,
    SideMenuModule,
    TlogLoadMenuModule,
    DropdownModule,
    MissionPlannerPropertiesModule,
    ModalModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly selectedPropertiesStorage = inject(SelectedTelemetryPropertiesStorage);

  protected readonly menuOpen = signal(true);
  protected readonly propertiesModalOpen = signal(false);
  protected readonly selectedProperties = signal<MissionPlannerProperty[]>(
    this.selectedPropertiesStorage.load(),
  );
  protected readonly flightSummaries = signal<FlightSummary[]>([]);
  protected readonly selectedFlightId = signal<string | null>(null);

  protected readonly selectedPropertyKeys = computed<MissionPlannerPropertyKey[]>(() =>
    [...this.selectedProperties()]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((property) => property.key),
  );

  protected readonly orderedSelectedProperties = computed(() =>
    [...this.selectedProperties()].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
  );

  protected readonly flightOptions = computed<DropdownOption[]>(() =>
    this.flightSummaries().map((flight, index) => ({
      value: flight.id,
      label: this.formatFlightOption(flight, index),
    })),
  );

  private readonly map = viewChild(MapComponent);

  constructor() {
    effect(() => {
      this.menuOpen();
      window.setTimeout(() => this.map()?.invalidateSize(), 220);
    });
  }

  protected openPropertiesModal(): void {
    this.propertiesModalOpen.set(true);
  }

  protected onPropertiesSaved(result: ModalCloseResult): void {
    const value = result.value as MissionPlannerProperty[] | undefined;
    if (value) {
      this.persistSelectedProperties(value);
    }
  }

  protected onPropertiesReordered(properties: MissionPlannerProperty[]): void {
    this.persistSelectedProperties(properties);
  }

  protected onPropertiesCancelled(): void {
    // Selection is reset when the modal content is recreated on next open.
  }

  protected onTlogUploaded(result: TlogUploadResult): void {
    this.flightSummaries.set(result.flights);
    this.selectedFlightId.set(result.flights[0]?.id ?? null);
  }

  private persistSelectedProperties(properties: MissionPlannerProperty[]): void {
    this.selectedProperties.set(properties);
    this.selectedPropertiesStorage.save(properties);
  }

  private formatFlightOption(flight: FlightSummary, index: number): string {
    const duration = this.formatDuration(flight.durationSeconds);
    return `Flight ${index + 1} · ${duration} · ${flight.messageCount} msgs`;
  }

  private formatDuration(seconds: number): string {
    const total = Math.max(0, Math.round(seconds));
    const minutes = Math.floor(total / 60);
    const remainder = total % 60;
    return minutes > 0 ? `${minutes}m ${remainder}s` : `${remainder}s`;
  }
}
