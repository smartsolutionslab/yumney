import { ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { UrlImportComponent } from './url-import.component';
import { FormFieldComponent } from '@yumney/ui';
import { setupTranslocoTesting } from '@yumney/shared/models';
import {
  RecipeApiService,
  ImportRecipeResponse,
  ImportStreamEvent,
} from '@yumney/shared/api-client';

const mockRecipe: ImportRecipeResponse = {
  title: 'Pasta Carbonara',
  description: 'A classic Italian pasta dish',
  ingredients: [{ name: 'Spaghetti', amount: 400, unit: 'g' }],
  steps: [{ number: 1, description: 'Cook pasta' }],
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
};

const en = {
  dashboard: {
    import: {
      title: 'Import a Recipe',
      subtitle: 'Paste a URL from any recipe website',
      placeholder: 'https://example.com/recipe/...',
      urlLabel: 'Recipe URL',
      submit: 'Import Recipe',
      submitting: 'Importing...',
      errors: {
        urlRequired: 'Please enter a URL.',
        urlInvalid: 'Please enter a valid HTTP or HTTPS URL.',
        urlTooLong: 'URL must not exceed 2048 characters.',
        generic: 'An unexpected error occurred. Please try again later.',
      },
    },
  },
};

function successStream(recipe: ImportRecipeResponse = mockRecipe) {
  return of(
    { type: 'status' as const, data: 'Fetching page...' },
    { type: 'status' as const, data: 'Extracting recipe...' },
    { type: 'done' as const, data: JSON.stringify(recipe) },
  );
}

describe('UrlImportComponent', () => {
  let component: UrlImportComponent;
  let fixture: ComponentFixture<UrlImportComponent>;
  let recipeApiMock: {
    importRecipeStream: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    recipeApiMock = { importRecipeStream: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        UrlImportComponent,
        setupTranslocoTesting(en),
      ],
      providers: [{ provide: RecipeApiService, useValue: recipeApiMock }],
    })
      .overrideComponent(FormFieldComponent, {
        set: { changeDetection: ChangeDetectionStrategy.Default },
      })
      .compileComponents();

    fixture = TestBed.createComponent(UrlImportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render the URL input', () => {
    const input = fixture.nativeElement.querySelector('input#url');
    expect(input).toBeTruthy();
  });

  it('should show validation error when submitting empty URL', () => {
    component.onSubmit();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Please enter a URL.');
  });

  it('should show validation error for invalid URL format', () => {
    component.form.controls.url.setValue('not-a-url');
    component.onSubmit();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Please enter a valid HTTP or HTTPS URL.');
  });

  it('should accept valid HTTP URL', () => {
    component.form.controls.url.setValue('http://example.com/recipe');
    expect(component.form.valid).toBe(true);
  });

  it('should accept valid HTTPS URL', () => {
    component.form.controls.url.setValue('https://example.com/recipe');
    expect(component.form.valid).toBe(true);
  });

  it('should accept URL with query parameters', () => {
    component.form.controls.url.setValue('https://example.com/recipe?id=123&lang=en');
    expect(component.form.valid).toBe(true);
  });

  it('should reject URL exceeding 2048 characters', () => {
    const longUrl = 'https://example.com/' + 'a'.repeat(2048);
    component.form.controls.url.setValue(longUrl);
    expect(component.form.valid).toBe(false);
  });

  it('should show validation error for URL exceeding max length', () => {
    const longUrl = 'https://example.com/' + 'a'.repeat(2048);
    component.form.controls.url.setValue(longUrl);
    component.onSubmit();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('URL must not exceed 2048 characters.');
  });

  it('should reject ftp URL', () => {
    component.form.controls.url.setValue('ftp://example.com/file');
    expect(component.form.valid).toBe(false);
  });

  it('should call recipeApi.importRecipeStream on valid submit', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should not call API when form is invalid', () => {
    component.onSubmit();
    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should mark fields as touched on invalid submit', () => {
    component.onSubmit();
    expect(component.form.controls.url.touched).toBe(true);
  });

  it('should show loading indicator during import', () => {
    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.textContent).toContain('Importing...');

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
  });

  it('should disable submit button while loading', () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);
  });

  it('should disable submit button while saving', () => {
    fixture.componentRef.setInput('isSaving', true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);
  });

  it('should emit extracted event with recipe and sourceUrl on done event', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    const extractedSpy = vi.fn();
    component.extracted.subscribe(extractedSpy);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(extractedSpy).toHaveBeenCalledWith({
      recipe: mockRecipe,
      sourceUrl: 'https://example.com/recipe',
    });
  }));

  it('should emit importStarted event on submit', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    const startedSpy = vi.fn();
    component.importStarted.subscribe(startedSpy);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(startedSpy).toHaveBeenCalled();
  }));

  it('should reset form after successful import', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(component.form.controls.url.value).toBe('');
  }));

  it('should emit failed event on stream error', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
    );
    const failedSpy = vi.fn();
    component.failed.subscribe(failedSpy);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(failedSpy).toHaveBeenCalledWith('dashboard.import.errors.generic');
  }));

  it('should set isLoading to false after error', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should emit failed on fail stream event', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      of(
        { type: 'status' as const, data: 'Fetching page...' },
        { type: 'fail' as const, data: 'Server error' },
      ),
    );
    const failedSpy = vi.fn();
    component.failed.subscribe(failedSpy);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(failedSpy).toHaveBeenCalledWith('dashboard.import.errors.generic');
    expect(component.isLoading()).toBe(false);
  }));

  it('should update streamingStatus on status events', fakeAsync(() => {
    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();

    subject.next({ type: 'status', data: 'Fetching page...' });
    expect(component.streamingStatus()).toBe('Fetching page...');

    subject.next({ type: 'status', data: 'Extracting recipe...' });
    expect(component.streamingStatus()).toBe('Extracting recipe...');

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
    tick();

    expect(component.streamingStatus()).toBeNull();
  }));

  it('should accumulate chunk events', fakeAsync(() => {
    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();

    subject.next({ type: 'chunk', data: 'first ' });
    subject.next({ type: 'chunk', data: 'second' });

    expect(component.streamingChunks()).toBe('first second');

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
    tick();
  }));

  it('should emit failed when done event has invalid JSON', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      of({ type: 'done' as const, data: 'not-valid-json{' }),
    );
    const failedSpy = vi.fn();
    component.failed.subscribe(failedSpy);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onSubmit();
    tick();

    expect(failedSpy).toHaveBeenCalledWith('dashboard.import.errors.generic');
  }));

  it('should set the URL and submit when importUrl is called', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.importUrl('https://shared.example.com/recipe');
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith(
      'https://shared.example.com/recipe',
    );
  }));
});
