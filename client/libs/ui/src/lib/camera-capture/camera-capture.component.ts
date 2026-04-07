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
import { CameraService, type FacingMode } from '@yumney/shared/models';
import { springPress, prefersReducedMotion } from '../animation/gsap-utils';

interface CapturedPhoto {
  id: string;
  blob: Blob;
  url: string;
}

@Component({
  selector: 'yn-camera-capture',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './camera-capture.component.html',
  styleUrl: './camera-capture.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CameraCaptureComponent implements AfterViewInit, OnDestroy {
  capturedReady = output<File[]>();
  cancelled = output<void>();
  fallbackRequested = output<void>();

  videoRef = viewChild<ElementRef<HTMLVideoElement>>('video');
  captureBtn = viewChild<ElementRef<HTMLButtonElement>>('captureBtn');

  protected camera = inject(CameraService);
  private destroyRef = inject(DestroyRef);

  protected facingMode = signal<FacingMode>('environment');
  protected error = signal<string | null>(null);
  protected isStreaming = signal(false);
  protected captures = signal<CapturedPhoto[]>([]);

  ngAfterViewInit(): void {
    if (!this.camera.cameraSupported()) {
      this.error.set('camera.errors.notSupported');
      this.fallbackRequested.emit();
      return;
    }
    this.startStream();
  }

  ngOnDestroy(): void {
    this.releaseAllPhotos();
    this.camera.stopCamera();
  }

  protected onCapture(): void {
    const video = this.videoRef()?.nativeElement;
    if (!video) return;

    const btn = this.captureBtn()?.nativeElement;
    if (btn && !prefersReducedMotion()) {
      springPress(btn);
    }

    this.camera
      .captureFrame(video)
      .then((blob) => {
        const url = URL.createObjectURL(blob);
        const id = `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
        this.captures.update((list) => [...list, { id, blob, url }]);
      })
      .catch(() => this.error.set('camera.errors.captureFailed'));
  }

  protected onToggleFacing(): void {
    const next: FacingMode = this.facingMode() === 'environment' ? 'user' : 'environment';
    this.facingMode.set(next);
    this.startStream();
  }

  protected onDeleteCapture(id: string): void {
    this.captures.update((list) => {
      const photo = list.find((p) => p.id === id);
      if (photo) URL.revokeObjectURL(photo.url);
      return list.filter((p) => p.id !== id);
    });
  }

  protected onDone(): void {
    const photos = this.captures();
    if (photos.length === 0) return;

    const files = photos.map((p, i) => this.camera.blobToFile(p.blob, `scan-${i + 1}.jpg`));
    this.capturedReady.emit(files);
  }

  protected onCancel(): void {
    this.cancelled.emit();
  }

  protected onUseGallery(): void {
    this.fallbackRequested.emit();
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
                // Autoplay may be blocked, that's fine
              });
            }
            this.isStreaming.set(true);
          }
        },
        error: () => {
          this.error.set('camera.errors.permissionDenied');
          this.fallbackRequested.emit();
        },
      });
  }

  private releaseAllPhotos(): void {
    this.captures().forEach((p) => URL.revokeObjectURL(p.url));
  }
}
