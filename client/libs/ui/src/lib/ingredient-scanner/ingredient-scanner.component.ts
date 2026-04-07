import {
  Component,
  ChangeDetectionStrategy,
  ElementRef,
  inject,
  output,
  signal,
  viewChild,
  AfterViewInit,
  OnDestroy,
  DestroyRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule } from '@jsverse/transloco';
import {
  CameraService,
  IngredientRecognitionService,
  type FacingMode,
} from '@yumney/shared/models';
import type { RecognizedIngredient } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-ingredient-scanner',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './ingredient-scanner.component.html',
  styleUrl: './ingredient-scanner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IngredientScannerComponent implements AfterViewInit, OnDestroy {
  ingredientsConfirmed = output<RecognizedIngredient[]>();
  cancelled = output<void>();

  videoRef = viewChild<ElementRef<HTMLVideoElement>>('video');

  protected camera = inject(CameraService);
  protected recognition = inject(IngredientRecognitionService);
  private destroyRef = inject(DestroyRef);

  protected facingMode = signal<FacingMode>('environment');
  protected error = signal<string | null>(null);
  protected isStreaming = signal(false);
  protected isScanning = signal(false);
  protected ingredients = signal<RecognizedIngredient[]>([]);
  protected lastScanAt = signal<number>(0);

  ngAfterViewInit(): void {
    if (!this.camera.cameraSupported()) {
      this.error.set('scanner.errors.notSupported');
      return;
    }
    this.startStream();
  }

  ngOnDestroy(): void {
    this.camera.stopCamera();
  }

  protected onScan(): void {
    const video = this.videoRef()?.nativeElement;
    if (!video || this.isScanning()) return;

    this.isScanning.set(true);
    this.error.set(null);

    this.camera
      .captureFrame(video)
      .then((blob) => {
        this.recognition
          .recognize(blob)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (response) => {
              const merged = this.recognition.mergeIngredients(
                this.ingredients(),
                response.ingredients,
              );
              this.ingredients.set(merged);
              this.lastScanAt.set(Date.now());
              this.isScanning.set(false);

              if (response.ingredients.length === 0) {
                this.error.set('scanner.errors.nothingDetected');
              }
            },
            error: () => {
              this.error.set('scanner.errors.recognitionFailed');
              this.isScanning.set(false);
            },
          });
      })
      .catch(() => {
        this.error.set('scanner.errors.captureFailed');
        this.isScanning.set(false);
      });
  }

  protected onRemoveIngredient(name: string): void {
    this.ingredients.update((list) => list.filter((i) => i.name !== name));
  }

  protected onToggleFacing(): void {
    const next: FacingMode = this.facingMode() === 'environment' ? 'user' : 'environment';
    this.facingMode.set(next);
    this.startStream();
  }

  protected onConfirm(): void {
    if (this.ingredients().length === 0) return;
    this.ingredientsConfirmed.emit(this.ingredients());
  }

  protected onCancel(): void {
    this.cancelled.emit();
  }

  protected confidenceLevel(confidence: number): 'high' | 'medium' | 'low' {
    return this.recognition.confidenceLevel(confidence);
  }

  private startStream(): void {
    this.error.set(null);
    this.camera
      .openCamera(this.facingMode())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stream) => {
          const video = this.videoRef()?.nativeElement;
          if (video) {
            video.srcObject = stream;
            const playResult = video.play?.();
            if (playResult?.catch) {
              playResult.catch(() => {
                // Autoplay may be blocked
              });
            }
            this.isStreaming.set(true);
          }
        },
        error: () => {
          this.error.set('scanner.errors.permissionDenied');
        },
      });
  }
}
