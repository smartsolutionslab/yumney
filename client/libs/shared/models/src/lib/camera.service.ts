import { Injectable, signal } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';

export type FacingMode = 'user' | 'environment';

@Injectable({ providedIn: 'root' })
export class CameraService {
  readonly cameraSupported = signal(this.detectSupport());

  private currentStream: MediaStream | null = null;

  openCamera(facingMode: FacingMode = 'environment'): Observable<MediaStream> {
    if (!this.cameraSupported()) {
      return throwError(() => new Error('Camera not supported'));
    }

    return from(this.requestStream(facingMode));
  }

  captureFrame(video: HTMLVideoElement): Promise<Blob> {
    return new Promise((resolve, reject) => {
      const canvas = document.createElement('canvas');
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const ctx = canvas.getContext('2d');
      if (!ctx) {
        reject(new Error('Failed to get canvas context'));
        return;
      }
      ctx.drawImage(video, 0, 0);
      canvas.toBlob(
        (blob) => {
          if (blob) {
            resolve(blob);
          } else {
            reject(new Error('Failed to create blob from canvas'));
          }
        },
        'image/jpeg',
        0.9,
      );
    });
  }

  stopCamera(): void {
    if (this.currentStream) {
      this.currentStream.getTracks().forEach((track) => track.stop());
      this.currentStream = null;
    }
  }

  blobToFile(blob: Blob, filename: string): File {
    return new File([blob], filename, { type: blob.type, lastModified: Date.now() });
  }

  private async requestStream(facingMode: FacingMode): Promise<MediaStream> {
    this.stopCamera();
    const stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode, width: { ideal: 1920 }, height: { ideal: 1080 } },
      audio: false,
    });
    this.currentStream = stream;
    return stream;
  }

  private detectSupport(): boolean {
    if (typeof navigator === 'undefined') return false;
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
  }
}
