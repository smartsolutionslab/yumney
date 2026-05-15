import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { CameraCaptureComponent } from './camera-capture.component';
import { CameraService, setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  camera: {
    capture: 'Capture',
    cancel: 'Cancel',
    done: 'Done ({{ count }})',
    deletePhoto: 'Delete photo',
    useGallery: 'Use gallery instead',
    guideHint: 'Center the recipe in the frame',
    errors: {
      notSupported: 'Camera not supported',
      permissionDenied: 'Camera permission denied',
      captureFailed: 'Capture failed',
    },
  },
};

interface CameraServiceMock {
  cameraSupported: ReturnType<typeof signal<boolean>>;
  openCamera: ReturnType<typeof vi.fn>;
  captureFrame: ReturnType<typeof vi.fn>;
  stopCamera: ReturnType<typeof vi.fn>;
  blobToFile: ReturnType<typeof vi.fn>;
}

function createMock(): CameraServiceMock {
  return {
    cameraSupported: signal(true),
    openCamera: vi.fn().mockReturnValue(of({ getTracks: () => [] } as unknown as MediaStream)),
    captureFrame: vi.fn().mockResolvedValue(new Blob(['x'], { type: 'image/jpeg' })),
    stopCamera: vi.fn(),
    blobToFile: vi
      .fn()
      .mockImplementation((blob: Blob, name: string) => new File([blob], name, { type: blob.type, lastModified: Date.now() })),
  };
}

async function setupComponent(cameraServiceMock: CameraServiceMock): Promise<ComponentFixture<CameraCaptureComponent>> {
  await TestBed.resetTestingModule()
    .configureTestingModule({
      imports: [CameraCaptureComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), { provide: CameraService, useValue: cameraServiceMock }],
    })
    .compileComponents();

  return TestBed.createComponent(CameraCaptureComponent);
}

describe('CameraCaptureComponent', () => {
  let fixture: ComponentFixture<CameraCaptureComponent>;
  let cameraServiceMock: CameraServiceMock;

  beforeEach(async () => {
    cameraServiceMock = createMock();
    fixture = await setupComponent(cameraServiceMock);
  });

  describe('initialization', () => {
    it('should create the component', () => {
      expect(fixture.componentInstance).toBeTruthy();
    });

    it('should request camera stream on init when supported', () => {
      fixture.detectChanges();
      expect(cameraServiceMock.openCamera).toHaveBeenCalledWith('environment');
    });

    it('should emit fallbackRequested when camera is not supported', async () => {
      cameraServiceMock.cameraSupported.set(false);
      const fallbackFixture = await setupComponent(cameraServiceMock);
      const fallbackSpy = vi.fn();
      fallbackFixture.componentInstance.fallbackRequested.subscribe(fallbackSpy);

      fallbackFixture.detectChanges();

      expect(fallbackSpy).toHaveBeenCalled();
    });

    it('should set error and emit fallback when permission is denied', async () => {
      cameraServiceMock.openCamera = vi.fn().mockReturnValue(throwError(() => new Error('denied')));
      const errorFixture = await setupComponent(cameraServiceMock);
      const fallbackSpy = vi.fn();
      errorFixture.componentInstance.fallbackRequested.subscribe(fallbackSpy);

      errorFixture.detectChanges();
      await Promise.resolve();

      expect(fallbackSpy).toHaveBeenCalled();
    });
  });

  describe('capture flow', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should add a captured photo to the list on capture', async () => {
      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      expect(cameraServiceMock.captureFrame).toHaveBeenCalled();
      const thumbnails = fixture.nativeElement.querySelectorAll('.thumbnail');
      expect(thumbnails.length).toBe(1);
    });

    it('should support multi-frame capture', async () => {
      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      captureBtn.click();
      await Promise.resolve();
      captureBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      const thumbnails = fixture.nativeElement.querySelectorAll('.thumbnail');
      expect(thumbnails.length).toBe(3);
    });

    it('should set error when captureFrame fails', async () => {
      cameraServiceMock.captureFrame = vi.fn().mockRejectedValue(new Error('fail'));
      const errorFixture = await setupComponent(cameraServiceMock);
      errorFixture.detectChanges();

      const captureBtn = errorFixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      errorFixture.detectChanges();

      const error = errorFixture.nativeElement.querySelector('.camera-error');
      expect(error).toBeTruthy();
    });
  });

  describe('photo management', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should remove a photo when delete button is clicked', async () => {
      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      captureBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      const deleteBtn = fixture.nativeElement.querySelector('.thumbnail-delete');
      deleteBtn.click();
      fixture.detectChanges();

      const thumbnails = fixture.nativeElement.querySelectorAll('.thumbnail');
      expect(thumbnails.length).toBe(1);
    });

    it('should revoke object URL when deleting a photo', async () => {
      const revokeSpy = vi.spyOn(URL, 'revokeObjectURL');

      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      const deleteBtn = fixture.nativeElement.querySelector('.thumbnail-delete');
      deleteBtn.click();

      expect(revokeSpy).toHaveBeenCalled();
    });
  });

  describe('done action', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should emit captured files via done', async () => {
      const capturedSpy = vi.fn();
      fixture.componentInstance.capturedReady.subscribe(capturedSpy);

      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      captureBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      const doneBtn = fixture.nativeElement.querySelector('.done-btn');
      doneBtn.click();

      expect(capturedSpy).toHaveBeenCalled();
      const files: File[] = capturedSpy.mock.calls[0][0];
      expect(files.length).toBe(2);
      expect(cameraServiceMock.blobToFile).toHaveBeenCalledTimes(2);
    });

    it('should not emit when no photos captured', () => {
      const capturedSpy = vi.fn();
      fixture.componentInstance.capturedReady.subscribe(capturedSpy);

      // Done button is not rendered when no captures
      const doneBtn = fixture.nativeElement.querySelector('.done-btn');
      expect(doneBtn).toBeFalsy();
      expect(capturedSpy).not.toHaveBeenCalled();
    });
  });

  describe('cancel and gallery', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should emit cancelled when cancel button is clicked', () => {
      const cancelSpy = vi.fn();
      fixture.componentInstance.cancelled.subscribe(cancelSpy);

      const cancelBtn = fixture.nativeElement.querySelector('.cancel-btn');
      cancelBtn.click();

      expect(cancelSpy).toHaveBeenCalled();
    });

    it('should emit fallbackRequested when gallery button is clicked', () => {
      const fallbackSpy = vi.fn();
      fixture.componentInstance.fallbackRequested.subscribe(fallbackSpy);

      const galleryBtn = fixture.nativeElement.querySelector('.gallery-fallback');
      galleryBtn.click();

      expect(fallbackSpy).toHaveBeenCalled();
    });
  });

  describe('camera flip', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should toggle facing mode and request new stream', () => {
      cameraServiceMock.openCamera.mockClear();

      const flipBtn = fixture.nativeElement.querySelector('.flip-btn');
      flipBtn.click();

      expect(cameraServiceMock.openCamera).toHaveBeenCalledWith('user');

      flipBtn.click();
      expect(cameraServiceMock.openCamera).toHaveBeenLastCalledWith('environment');
    });
  });

  describe('cleanup', () => {
    it('should call stopCamera on destroy', () => {
      fixture.detectChanges();
      fixture.destroy();
      expect(cameraServiceMock.stopCamera).toHaveBeenCalled();
    });

    it('should revoke all photo URLs on destroy', async () => {
      fixture.detectChanges();
      const revokeSpy = vi.spyOn(URL, 'revokeObjectURL');

      const captureBtn = fixture.nativeElement.querySelector('.capture-btn');
      captureBtn.click();
      await Promise.resolve();
      captureBtn.click();
      await Promise.resolve();

      revokeSpy.mockClear();
      fixture.destroy();

      expect(revokeSpy.mock.calls.length).toBeGreaterThanOrEqual(2);
    });
  });
});
