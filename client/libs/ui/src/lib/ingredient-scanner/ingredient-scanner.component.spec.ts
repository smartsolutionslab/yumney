import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { IngredientScannerComponent } from './ingredient-scanner.component';
import { CameraService, IngredientRecognitionService, setupTranslocoTesting } from '@yumney/shared/models';
import type {
  RecognizedIngredient,
  RecognizedIngredientsResponse,
} from '@yumney/shared/api-client';

const en = {
  scanner: {
    scan: 'Scan',
    cancel: 'Cancel',
    removeIngredient: 'Remove',
    useIngredients: 'Use {{ count }} ingredients',
    errors: {
      notSupported: 'Not supported',
      permissionDenied: 'Permission denied',
      captureFailed: 'Capture failed',
      recognitionFailed: 'Recognition failed',
      nothingDetected: 'Nothing detected',
    },
  },
};

interface CameraServiceMock {
  cameraSupported: ReturnType<typeof signal<boolean>>;
  openCamera: ReturnType<typeof vi.fn>;
  captureFrame: ReturnType<typeof vi.fn>;
  stopCamera: ReturnType<typeof vi.fn>;
}

interface RecognitionServiceMock {
  recognize: ReturnType<typeof vi.fn>;
  mergeIngredients: ReturnType<typeof vi.fn>;
  confidenceLevel: ReturnType<typeof vi.fn>;
}

function createCameraMock(): CameraServiceMock {
  return {
    cameraSupported: signal(true),
    openCamera: vi.fn().mockReturnValue(of({ getTracks: () => [] } as unknown as MediaStream)),
    captureFrame: vi.fn().mockResolvedValue(new Blob(['x'], { type: 'image/jpeg' })),
    stopCamera: vi.fn(),
  };
}

function createRecognitionMock(): RecognitionServiceMock {
  return {
    recognize: vi.fn().mockReturnValue(
      of({
        ingredients: [{ name: 'Tomato', confidence: 0.9, category: 'produce' }],
      } satisfies RecognizedIngredientsResponse),
    ),
    mergeIngredients: vi
      .fn()
      .mockImplementation((existing: RecognizedIngredient[], incoming: RecognizedIngredient[]) => [
        ...existing,
        ...incoming,
      ]),
    confidenceLevel: vi.fn().mockReturnValue('high'),
  };
}

async function setup(
  cameraMock: CameraServiceMock,
  recognitionMock: RecognitionServiceMock,
): Promise<ComponentFixture<IngredientScannerComponent>> {
  await TestBed.resetTestingModule()
    .configureTestingModule({
      imports: [
        IngredientScannerComponent,
        setupTranslocoTesting(en),
      ],
      providers: [
        { provide: CameraService, useValue: cameraMock },
        { provide: IngredientRecognitionService, useValue: recognitionMock },
      ],
    })
    .compileComponents();

  return TestBed.createComponent(IngredientScannerComponent);
}

describe('IngredientScannerComponent', () => {
  let fixture: ComponentFixture<IngredientScannerComponent>;
  let cameraMock: CameraServiceMock;
  let recognitionMock: RecognitionServiceMock;

  beforeEach(async () => {
    cameraMock = createCameraMock();
    recognitionMock = createRecognitionMock();
    fixture = await setup(cameraMock, recognitionMock);
  });

  describe('initialization', () => {
    it('should create the component', () => {
      expect(fixture.componentInstance).toBeTruthy();
    });

    it('should request camera stream when supported', () => {
      fixture.detectChanges();
      expect(cameraMock.openCamera).toHaveBeenCalledWith('environment');
    });

    it('should set error when camera is not supported', async () => {
      cameraMock.cameraSupported.set(false);
      const noCamFixture = await setup(cameraMock, recognitionMock);
      noCamFixture.detectChanges();
      expect(noCamFixture.componentInstance['error']()).toBe('scanner.errors.notSupported');
    });

    it('should set error when permission is denied', async () => {
      cameraMock.openCamera = vi.fn().mockReturnValue(throwError(() => new Error('denied')));
      const errorFixture = await setup(cameraMock, recognitionMock);
      errorFixture.detectChanges();
      await Promise.resolve();
      expect(errorFixture.componentInstance['error']()).toBe('scanner.errors.permissionDenied');
    });
  });

  describe('scanning', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should capture and recognize on scan button click', async () => {
      const scanBtn = fixture.nativeElement.querySelector('.scan-btn');
      scanBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      fixture.detectChanges();

      expect(cameraMock.captureFrame).toHaveBeenCalled();
      expect(recognitionMock.recognize).toHaveBeenCalled();
      expect(recognitionMock.mergeIngredients).toHaveBeenCalled();
    });

    it('should add detected ingredients to the list', async () => {
      const scanBtn = fixture.nativeElement.querySelector('.scan-btn');
      scanBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      fixture.detectChanges();

      expect(fixture.componentInstance['ingredients']().length).toBe(1);
    });

    it('should show error when nothing is detected', async () => {
      recognitionMock.recognize = vi.fn().mockReturnValue(of({ ingredients: [] }));
      const emptyFixture = await setup(cameraMock, recognitionMock);
      emptyFixture.detectChanges();

      const scanBtn = emptyFixture.nativeElement.querySelector('.scan-btn');
      scanBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      emptyFixture.detectChanges();

      expect(emptyFixture.componentInstance['error']()).toBe('scanner.errors.nothingDetected');
    });

    it('should show error when recognition API fails', async () => {
      recognitionMock.recognize = vi.fn().mockReturnValue(throwError(() => new Error('fail')));
      const failFixture = await setup(cameraMock, recognitionMock);
      failFixture.detectChanges();

      const scanBtn = failFixture.nativeElement.querySelector('.scan-btn');
      scanBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      failFixture.detectChanges();

      expect(failFixture.componentInstance['error']()).toBe('scanner.errors.recognitionFailed');
    });
  });

  describe('ingredient management', () => {
    beforeEach(async () => {
      fixture.detectChanges();
      const scanBtn = fixture.nativeElement.querySelector('.scan-btn');
      scanBtn.click();
      await Promise.resolve();
      await Promise.resolve();
      fixture.detectChanges();
    });

    it('should remove an ingredient when remove button is clicked', () => {
      const removeBtn = fixture.nativeElement.querySelector('.result-remove');
      removeBtn.click();
      fixture.detectChanges();
      expect(fixture.componentInstance['ingredients']().length).toBe(0);
    });

    it('should emit ingredientsConfirmed when confirm is clicked', () => {
      const confirmedSpy = vi.fn();
      fixture.componentInstance.ingredientsConfirmed.subscribe(confirmedSpy);

      const confirmBtn = fixture.nativeElement.querySelector('.confirm-btn');
      confirmBtn.click();

      expect(confirmedSpy).toHaveBeenCalled();
      expect(confirmedSpy.mock.calls[0][0].length).toBe(1);
    });

    it('should not show confirm button when no ingredients', () => {
      fixture.componentInstance['ingredients'].set([]);
      fixture.detectChanges();
      const confirmBtn = fixture.nativeElement.querySelector('.confirm-btn');
      expect(confirmBtn).toBeFalsy();
    });
  });

  describe('cancel and flip', () => {
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

    it('should toggle facing mode when flip is clicked', () => {
      cameraMock.openCamera.mockClear();
      const flipBtn = fixture.nativeElement.querySelector('.flip-btn');
      flipBtn.click();
      expect(cameraMock.openCamera).toHaveBeenCalledWith('user');
    });
  });

  describe('cleanup', () => {
    it('should call stopCamera on destroy', () => {
      fixture.detectChanges();
      fixture.destroy();
      expect(cameraMock.stopCamera).toHaveBeenCalled();
    });
  });
});
