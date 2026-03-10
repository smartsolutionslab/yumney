import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import {
  RecipeApiService,
  ImportRecipeResponse,
  SavedRecipeResponse,
} from '@yumney/shared/api-client';

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
    save: {
      success: 'Recipe "{{title}}" saved successfully!',
      saving: 'Saving...',
      errors: {
        duplicate: 'This recipe has already been imported.',
        generic: 'Failed to save recipe. Please try again.',
      },
    },
    preview: {
      title: 'Review Extracted Recipe',
      recipeTitle: 'Title',
      description: 'Description',
      servings: 'Servings',
      prepTime: 'Prep Time (min)',
      cookTime: 'Cook Time (min)',
      difficulty: 'Difficulty',
      ingredients: 'Ingredients',
      ingredientName: 'Ingredient',
      amount: 'Amount',
      unit: 'Unit',
      addIngredient: 'Add Ingredient',
      steps: 'Steps',
      stepDescription: 'Step Description',
      addStep: 'Add Step',
      save: 'Save Recipe',
      discard: 'Discard',
      errors: {
        titleRequired: 'Title is required.',
        titleMaxLength: 'Title must not exceed 200 characters.',
        ingredientNameRequired: 'Ingredient name is required.',
        stepDescriptionRequired: 'Step description is required.',
      },
    },
  },
};

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let recipeApiMock: {
    importRecipe: ReturnType<typeof vi.fn>;
    saveRecipe: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    recipeApiMock = { importRecipe: vi.fn(), saveRecipe: vi.fn() };

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

  it('should show recipe preview after successful extraction', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    const preview = fixture.nativeElement.querySelector('yn-recipe-preview');
    expect(preview).toBeTruthy();
  }));

  it('should not show recipe preview when no recipe is extracted', () => {
    const preview = fixture.nativeElement.querySelector('yn-recipe-preview');
    expect(preview).toBeNull();
  });

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

  it('should clear extracted recipe on discard', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    component.onDiscardRecipe();

    expect(component.extractedRecipe()).toBeNull();
  }));

  it('should hide recipe preview after discard', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeTruthy();

    component.onDiscardRecipe();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeNull();
  }));

  it('should call saveRecipe API on save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalled();
  }));

  it('should set saveSuccess on successful save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(component.saveSuccess()).toBe('Pasta Carbonara');
    expect(component.extractedRecipe()).toBeNull();
  }));

  it('should show duplicate error on 409 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(component.serverError()).toBe('dashboard.save.errors.duplicate');
  }));

  it('should show generic save error on 500 response', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(component.serverError()).toBe('dashboard.save.errors.generic');
  }));

  it('should set isSaving during save operation', fakeAsync(() => {
    const subject = new Subject<SavedRecipeResponse>();
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    expect(component.isSaving()).toBe(true);

    subject.next({
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    });
    subject.complete();
    tick();

    expect(component.isSaving()).toBe(false);
  }));

  it('should call saveRecipe without sourceUrl when sourceUrl is null', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.extractedRecipe.set(mockRecipe);
    component.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(
      expect.not.objectContaining({ sourceUrl: expect.anything() }),
    );
  }));

  it('should clear saveSuccess on new import', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    expect(component.saveSuccess()).toBe('Pasta Carbonara');

    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    component.form.controls.url.setValue('https://example.com/other');
    component.onImport();

    expect(component.saveSuccess()).toBeNull();
  }));

  it('should include sourceUrl in save request', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(
      expect.objectContaining({ sourceUrl: 'https://example.com/recipe' }),
    );
  }));

  it('should map recipe fields to save request correctly', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Pasta Carbonara',
        description: 'A classic Italian pasta dish',
        servings: 4,
        prepTimeMinutes: 10,
        cookTimeMinutes: 20,
        difficulty: 'medium',
        ingredients: [
          { name: 'Spaghetti', amount: 400, unit: 'g' },
          { name: 'Pancetta', amount: 200, unit: 'g' },
        ],
        steps: [
          { number: 1, description: 'Cook pasta' },
          { number: 2, description: 'Fry pancetta' },
        ],
      }),
    );
  }));

  it('should set isSaving to false after save error', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(component.isSaving()).toBe(false);
  }));

  it('should clear serverError when starting a save', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    expect(component.serverError()).toBe('dashboard.save.errors.generic');

    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));
    component.onSaveRecipe(mockRecipe);
    expect(component.serverError()).toBeNull();

    tick();
  }));

  it('should disable import button while saving', fakeAsync(() => {
    const subject = new Subject<SavedRecipeResponse>();
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.disabled).toBe(true);

    subject.next({
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    });
    subject.complete();
    tick();
  }));

  it('should clear serverError on discard', fakeAsync(() => {
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    expect(component.serverError()).toBe('dashboard.save.errors.generic');

    component.onDiscardRecipe();

    expect(component.serverError()).toBeNull();
  }));

  it('should clear saveSuccess when starting a new save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    expect(component.saveSuccess()).toBe('Pasta Carbonara');

    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.sourceUrl.set('https://example.com/recipe');
    component.onSaveRecipe(mockRecipe);
    expect(component.saveSuccess()).toBeNull();

    tick();
  }));

  it('should show success banner after save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipe.mockReturnValue(of(mockRecipe));
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.success-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('Pasta Carbonara');
  }));
});
