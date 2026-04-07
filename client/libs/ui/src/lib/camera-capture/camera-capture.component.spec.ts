import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of } from 'rxjs';
import { CameraCaptureComponent } from './camera-capture.component';
import { CameraService } from '@yumney/shared/models';

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

describe('CameraCaptureComponent', () => {
  let fixture: ComponentFixture<CameraCaptureComponent>;
  let cameraServiceMock: {
    cameraSupported: ReturnType<typeof signal<boolean>>;
    openCamera: ReturnType<typeof vi.fn>;
    captureFrame: ReturnType<typeof vi.fn>;
    stopCamera: ReturnType<typeof vi.fn>;
    blobToFile: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    cameraServiceMock = {
      cameraSupported: signal(true),
      openCamera: vi.fn().mockReturnValue(of({ getTracks: () => [] } as unknown as MediaStream)),
      captureFrame: vi.fn(),
      stopCamera: vi.fn(),
      blobToFile: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [
        CameraCaptureComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
        }),
      ],
      providers: [{ provide: CameraService, useValue: cameraServiceMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(CameraCaptureComponent);
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should emit fallbackRequested when camera is not supported', () => {
    cameraServiceMock.cameraSupported.set(false);
    const fallbackFixture = TestBed.createComponent(CameraCaptureComponent);
    const fallbackSpy = vi.fn();
    fallbackFixture.componentInstance.fallbackRequested.subscribe(fallbackSpy);
    fallbackFixture.detectChanges();
    expect(fallbackSpy).toHaveBeenCalled();
  });

  it('should call stopCamera on destroy', () => {
    fixture.detectChanges();
    fixture.destroy();
    expect(cameraServiceMock.stopCamera).toHaveBeenCalled();
  });
});
