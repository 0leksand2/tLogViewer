import {
  Component,
  ElementRef,
  HostListener,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { SpinnerComponent } from '../../shared/spinner/components/spinner';
import { TlogService, TlogUploadResult } from '../services/tlog.service';

@Component({
  selector: 'app-tlog-load-menu',
  standalone: true,
  imports: [SpinnerComponent, TranslatePipe],
  templateUrl: './tlog-load-menu.html',
  styleUrl: './tlog-load-menu.scss',
})
export class TlogLoadMenuComponent {
  readonly uploaded = output<TlogUploadResult>();

  private readonly tlogService = inject(TlogService);
  private readonly translate = inject(TranslateService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly menuOpen = signal(false);
  protected readonly selectedFile = signal<File | null>(null);
  protected readonly uploading = signal(false);
  protected readonly statusMessage = signal<string | null>(null);
  protected readonly statusTone = signal<'ok' | 'error' | null>(null);
  /** When true, log is split into flights on arm cycles; default on. */
  protected readonly splitIntoFlights = signal(true);

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
    if (!this.menuOpen()) {
      return;
    }
    this.statusMessage.set(null);
    this.statusTone.set(null);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  openFilePicker(): void {
    this.fileInput().nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';

    if (!file) {
      return;
    }

    if (!file.name.toLowerCase().endsWith('.tlog')) {
      this.selectedFile.set(null);
      this.statusMessage.set(this.translate.instant('upload.chooseTlog'));
      this.statusTone.set('error');
      return;
    }

    this.selectedFile.set(file);
    this.statusMessage.set(null);
    this.statusTone.set(null);
  }

  onSplitIntoFlightsChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.splitIntoFlights.set(input.checked);
  }

  uploadSelected(): void {
    const file = this.selectedFile();
    if (!file || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.statusMessage.set(this.translate.instant('upload.uploading'));
    this.statusTone.set(null);

    this.tlogService.upload(file, this.splitIntoFlights()).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.statusMessage.set(
          this.translate.instant('upload.uploaded', {
            fileName: result.fileName,
            count: result.flightCount,
          }),
        );
        this.statusTone.set('ok');
        this.uploaded.emit(result);
      },
      error: (err: unknown) => {
        this.uploading.set(false);
        this.statusMessage.set(this.errorMessage(err));
        this.statusTone.set('error');
      },
    });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.menuOpen()) {
      return;
    }
    if (this.host.nativeElement.contains(event.target as Node)) {
      return;
    }
    this.closeMenu();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }

  private errorMessage(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as { message?: string } | string | null;
      if (typeof body === 'string' && body.trim()) {
        return body;
      }
      if (body && typeof body === 'object' && body.message) {
        return body.message;
      }
      if (err.status === 0) {
        return this.translate.instant('upload.serverUnreachable');
      }
      return this.translate.instant('upload.uploadFailedStatus', { status: err.status });
    }
    return this.translate.instant('upload.uploadFailed');
  }
}
