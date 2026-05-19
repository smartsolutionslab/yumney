import { firstValueFrom } from 'rxjs';
import { CameraService } from './camera.service';

describe('CameraService', () => {
  let service: CameraService;

  beforeEach(() => {
    // Ensure mediaDevices exists for support detection
    Object.defineProperty(navigator, 'mediaDevices', {
      value: { getUserMedia: vi.fn() },
      configurable: true,
      writable: true,
    });
    service = new CameraService();
  });

  describe('cameraSupported', () => {
    it('should expose camera support as a signal', () => {
      expect(typeof service.cameraSupported()).toBe('boolean');
    });

    it('should be true when mediaDevices is available', () => {
      expect(service.cameraSupported()).toBe(true);
    });

    it('should be false when mediaDevices is undefined', () => {
      Object.defineProperty(navigator, 'mediaDevices', {
        value: undefined,
        configurable: true,
      });
      const freshService = new CameraService();
      expect(freshService.cameraSupported()).toBe(false);
    });
  });

  describe('blobToFile', () => {
    it('should convert blob to file with correct name and type', () => {
      const blob = new Blob(['test'], { type: 'image/jpeg' });
      const file = service.blobToFile(blob, 'photo.jpg');

      expect(file).toBeInstanceOf(File);
      expect(file.name).toBe('photo.jpg');
      expect(file.type).toBe('image/jpeg');
      expect(file.size).toBe(4);
    });

    it('should set lastModified to a recent timestamp', () => {
      const before = Date.now();
      const file = service.blobToFile(new Blob(['x']), 'a.jpg');
      const after = Date.now();

      expect(file.lastModified).toBeGreaterThanOrEqual(before);
      expect(file.lastModified).toBeLessThanOrEqual(after);
    });

    it('should preserve png blob type', () => {
      const blob = new Blob(['png-data'], { type: 'image/png' });
      const file = service.blobToFile(blob, 'photo.png');
      expect(file.type).toBe('image/png');
    });
  });

  describe('stopCamera', () => {
    it('should not throw when no stream is active', () => {
      expect(() => service.stopCamera()).not.toThrow();
    });

    it('should stop all tracks of the active stream', async () => {
      const stopSpy1 = vi.fn();
      const stopSpy2 = vi.fn();
      const fakeStream = {
        getTracks: () => [{ stop: stopSpy1 } as unknown as MediaStreamTrack, { stop: stopSpy2 } as unknown as MediaStreamTrack],
      } as MediaStream;

      mockGetUserMedia(fakeStream);
      await firstValueFrom(service.openCamera('environment'));
      service.stopCamera();

      expect(stopSpy1).toHaveBeenCalled();
      expect(stopSpy2).toHaveBeenCalled();
    });
  });

  describe('openCamera', () => {
    it('should error when camera is not supported', async () => {
      Object.defineProperty(navigator, 'mediaDevices', {
        value: undefined,
        configurable: true,
      });
      const unsupportedService = new CameraService();

      await expect(firstValueFrom(unsupportedService.openCamera())).rejects.toThrow('Camera not supported');
    });

    it('should request stream with environment facing mode by default', async () => {
      const fakeStream = { getTracks: () => [] } as MediaStream;
      const getUserMediaSpy = mockGetUserMedia(fakeStream);

      await firstValueFrom(service.openCamera());

      expect(getUserMediaSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          video: expect.objectContaining({ facingMode: 'environment' }),
          audio: false,
        }),
      );
    });

    it('should request stream with user facing mode when specified', async () => {
      const fakeStream = { getTracks: () => [] } as MediaStream;
      const getUserMediaSpy = mockGetUserMedia(fakeStream);

      await firstValueFrom(service.openCamera('user'));

      expect(getUserMediaSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          video: expect.objectContaining({ facingMode: 'user' }),
        }),
      );
    });

    it('should stop previous stream when opening a new one', async () => {
      const stopSpy = vi.fn();
      const firstStream = {
        getTracks: () => [{ stop: stopSpy } as unknown as MediaStreamTrack],
      } as MediaStream;
      const secondStream = { getTracks: () => [] } as MediaStream;

      mockGetUserMedia(firstStream);
      await firstValueFrom(service.openCamera('environment'));

      mockGetUserMedia(secondStream);
      await firstValueFrom(service.openCamera('user'));

      expect(stopSpy).toHaveBeenCalled();
    });
  });

  describe('captureFrame', () => {
    it('should reject if canvas context is not available', async () => {
      const video = createMockVideo();
      vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValueOnce(null);

      await expect(service.captureFrame(video)).rejects.toThrow('canvas context');
    });

    it('should reject if blob creation fails', async () => {
      const video = createMockVideo();
      vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValueOnce({
        drawImage: vi.fn(),
      } as unknown as CanvasRenderingContext2D);
      vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementationOnce((cb) => cb(null));

      await expect(service.captureFrame(video)).rejects.toThrow('blob from canvas');
    });

    it('should resolve with blob when successful', async () => {
      const video = createMockVideo();
      const fakeBlob = new Blob(['x'], { type: 'image/jpeg' });
      vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValueOnce({
        drawImage: vi.fn(),
      } as unknown as CanvasRenderingContext2D);
      vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementationOnce((cb) => cb(fakeBlob));

      const result = await service.captureFrame(video);
      expect(result).toBe(fakeBlob);
    });
  });
});

function mockGetUserMedia(stream: MediaStream): ReturnType<typeof vi.fn> {
  const spy = vi.fn().mockResolvedValue(stream);
  Object.defineProperty(navigator, 'mediaDevices', {
    value: { getUserMedia: spy },
    configurable: true,
    writable: true,
  });
  return spy;
}

function createMockVideo(): HTMLVideoElement {
  const video = document.createElement('video');
  Object.defineProperty(video, 'videoWidth', { value: 1920, configurable: true });
  Object.defineProperty(video, 'videoHeight', { value: 1080, configurable: true });
  return video;
}
