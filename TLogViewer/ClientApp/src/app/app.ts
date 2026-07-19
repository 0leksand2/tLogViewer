import { Component, effect, inject, signal, viewChild } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MapComponent } from './map/components/map';
import { MapModule } from './map/map.module';
import { SideMenuModule } from './side-menu/side-menu.module';
import { TlogLoadMenuModule } from './tlog-load-menu/tlog-load-menu.module';
import { Flight, TlogUploadResult } from './tlog-load-menu/models/mav-message.models';
import { TlogService } from './tlog-load-menu/services/tlog.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MapModule, SideMenuModule, TlogLoadMenuModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly tlogService = inject(TlogService);

  protected readonly menuOpen = signal(true);
  protected readonly flights = signal<Flight[]>([]);
  private readonly map = viewChild(MapComponent);

  constructor() {
    effect(() => {
      this.menuOpen();
      window.setTimeout(() => this.map()?.invalidateSize(), 220);
    });
  }

  protected onTlogUploaded(result: TlogUploadResult): void {
    console.log('TLog uploaded', {
      sessionId: result.sessionId,
      fileName: result.fileName,
      flightCount: result.flightCount,
      flights: result.flights.map((f) => ({
        id: f.id,
        durationSeconds: f.durationSeconds,
        messageCount: f.messageCount,
      })),
    });

    if (result.flightCount === 0) {
      this.flights.set([]);
      return;
    }

    const requests = result.flights.map((summary) =>
      this.tlogService.getFlight(result.sessionId, summary.id),
    );

    forkJoin(requests).subscribe({
      next: (responses) => {
        const loaded = responses.map((r) => r.flight);
        this.flights.set(loaded);
        console.log('Flights loaded', {
          count: loaded.length,
          sessionReleased: responses.some((r) => r.sessionReleased),
          messageTotals: loaded.map((f) => ({ id: f.id, messages: f.messages })),
        });
      },
      error: (err) => console.error('Failed to load flights', err),
    });
  }
}
