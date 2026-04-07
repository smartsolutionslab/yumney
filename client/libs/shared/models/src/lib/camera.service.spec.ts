import { TestBed } from '@angular/core/testing';
import { CameraService } from './camera.service';

describe('CameraService', () => {
  let service: CameraService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CameraService);
  });

  it('should expose camera support signal', () => {
    expect(typeof service.cameraSupported()).toBe('boolean');
  });

  it('should convert blob to file with correct properties', () => {
    const blob = new Blob(['test'], { type: 'image/jpeg' });
    const file = service.blobToFile(blob, 'photo.jpg');

    expect(file.name).toBe('photo.jpg');
    expect(file.type).toBe('image/jpeg');
    expect(file.size).toBeGreaterThan(0);
  });

  it('should handle stopCamera when no stream is active', () => {
    expect(() => service.stopCamera()).not.toThrow();
  });
});
