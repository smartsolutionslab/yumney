import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { RecipeApiService, ImportRecipeResponse } from '@yumney/shared/api-client';

const mockRecipe: ImportRecipeResponse = {
  title: 'Pasta Carbonara',
  description: 'A classic Italian pasta dish',
  ingredients: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Pancetta', amount: 200, unit: 'g' },
  ],
  steps: [
    { number: 1, description: 'Cook pasta' },
    { number: 2, description: 'Fry pancetta' },
  ],
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
};

const en = {
  dashboard: {
    title: 'Dashboard',
    welcome: 'Welcome to Yumney!',
    import: {
      title: 'Import a Recipe',
      subtitle: 'Paste a URL from any recipe website',
      placeholder: 'https://example.com/recipe/...',
      submit: 'Import Recipe',
      submitting: 'Importing...',
      success: 'Recipe "{{title}}" extracted successfully!',
      errors: {
        urlRequired: 'Please enter a URL.',
        urlInvalid: 'Please enter a valid HTTP or HTTPS URL.',
        urlTooLong: 'URL must not exceed 2048 characters.',
        unreachable: 'Could not reach the website. Please check the URL.',
        timeout: 'Extraction timed out. Please try again.',
        noRecipe: 'No recipe found on this page.',
        generic: 'An unexpected error occurred. Please try again later.',
      },
    },
  },
};

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let recipeApiMock: { importRecipe: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    recipeApiMock = { importRecipe: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [{ provide: RecipeApiService, useValue: recipeApiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render the welcome message', () => {
    const element = fixture.nativeElement;
    expect(element.textContent).toContain('Welcome to Yumney!');
  });

  it('should render the dashboard title', () => {
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading.textContent).toContain('Dashboard');
  });

  it('should render the URL import input', () => {
    const input = fixture.nativeElement.querySelector('input#url');
    expect(input).toBeTruthy();
  });

  it('should show validation error when submitting empty URL', () => {
    component.onImport();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Please enter a URL.');
  });

  it('should show validation error for invalid URL format', () => {
    component.form.controls.url.setValue('not-a-url');
    component.onImport();
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
    component.onImport();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('URL must not exceed 2048 characters.');
  });

  it('should call recipeApi.importRecipe on valid submit', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(recipeApiMock.importRecipe).toHaveBeenCalledWith({
      url: 'https://example.com/recipe',
    });
  }));

  it('should show loading indicator during import', () => {
    const subject = new Subject();
    recipeApiMock.importRecipe.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.textContent).toContain('Importing...');

    subject.next(mockRecipe);
    subject.complete();
  });

  it('should show server error on API failure', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('.error-banner');
    expect(errorBanner.textContent).toContain('An unexpected error occurred.');
  }));

  it('should disable submit button while loading', () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);
  });

  it('should not call API when form is invalid', () => {
    component.onImport();

    expect(recipeApiMock.importRecipe).not.toHaveBeenCalled();
  });

  it('should mark fields as touched on invalid submit', () => {
    component.onImport();

    expect(component.form.controls.url.touched).toBe(true);
  });

  it('should reset form after successful import', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.form.controls.url.value).toBe('');
  }));

  it('should clear server error on new submission', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.serverError()).toBe('dashboard.import.errors.generic');

    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    expect(component.serverError()).toBeNull();
  }));

  it('should reject ftp URL', () => {
    component.form.controls.url.setValue('ftp://example.com/file');

    expect(component.form.valid).toBe(false);
  });

  it('should return false from hasError when control is not touched', () => {
    component.form.controls.url.setValue('');
    expect(component.hasError('url', 'required')).toBe(false);
  });

  it('should return true from hasError when control is touched and has error', () => {
    component.form.controls.url.setValue('');
    component.form.controls.url.markAsTouched();
    expect(component.hasError('url', 'required')).toBe(true);
  });

  it('should set isLoading to false after error', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should show success banner with recipe title after extraction', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    const successBanner = fixture.nativeElement.querySelector('.success-banner');
    expect(successBanner).toBeTruthy();
    expect(successBanner.textContent).toContain('Pasta Carbonara');
  }));

  it('should store extracted recipe data', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.extractedRecipe()).toEqual(mockRecipe);
  }));

  it('should show unreachable error on 502 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 502 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.unreachable');
  }));

  it('should show timeout error on 504 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 504 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.timeout');
  }));

  it('should show no recipe error on 404 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.noRecipe');
  }));

  it('should clear extracted recipe before new import', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    const subject = new Subject();
    recipeApiMock.importRecipe.mockReturnValue(subject);
    component.form.controls.url.setValue('https://example.com/other');
    component.onImport();

    expect(component.extractedRecipe()).toBeNull();

    subject.next(mockRecipe);
    subject.complete();
  }));

  it('should clear extracted recipe on error', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.form.controls.url.setValue('https://example.com/other');
    component.onImport();
    tick();

    expect(component.extractedRecipe()).toBeNull();
  }));

  it('should show generic error on 400 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 400 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should show generic error on 422 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 422 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));
});
