import { ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { FormFieldComponent } from '@yumney/ui';
import {
  RecipeApiService,
  ImportRecipeResponse,
  ImportStreamEvent,
  SavedRecipeResponse,
  DashboardApiService,
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
      urlLabel: 'Recipe URL',
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
    create: {
      title: 'Create a Recipe',
      subtitle: 'Start from scratch and enter your own recipe',
      submit: 'Create Recipe',
      previewTitle: 'New Recipe',
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

function streamEvents(...events: ImportStreamEvent[]): Subject<ImportStreamEvent> {
  return of(...events) as unknown as Subject<ImportStreamEvent>;
}

function successStream(recipe: ImportRecipeResponse = mockRecipe) {
  return of(
    { type: 'status' as const, data: 'Fetching page...' },
    { type: 'status' as const, data: 'Extracting recipe...' },
    { type: 'done' as const, data: JSON.stringify(recipe) },
  );
}

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let recipeApiMock: {
    importRecipe: ReturnType<typeof vi.fn>;
    importRecipeStream: ReturnType<typeof vi.fn>;
    saveRecipe: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };
  let activatedRouteMock: { snapshot: { queryParams: Record<string, string> } };

  beforeEach(async () => {
    recipeApiMock = {
      importRecipe: vi.fn(),
      importRecipeStream: vi.fn(),
      saveRecipe: vi.fn(),
    };
    routerMock = { navigate: vi.fn() };
    activatedRouteMock = { snapshot: { queryParams: {} } };

    const dashboardApiMock = {
      getSuggestions: vi.fn().mockReturnValue(of({ suggestions: [], quickActions: [] })),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };
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
      providers: [
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: DashboardApiService, useValue: dashboardApiMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock },
      ],
    })
      .overrideComponent(FormFieldComponent, {
        set: { changeDetection: ChangeDetectionStrategy.Default },
      })
      .compileComponents();

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

  it('should call recipeApi.importRecipeStream on valid submit', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should show loading indicator during import', () => {
    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]');
    expect(button.textContent).toContain('Importing...');

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
  });

  it('should show server error on API failure', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
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

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should mark fields as touched on invalid submit', () => {
    component.onImport();

    expect(component.form.controls.url.touched).toBe(true);
  });

  it('should reset form after successful import', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.form.controls.url.value).toBe('');
  }));

  it('should clear server error on new submission', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.serverError()).toBe('dashboard.import.errors.generic');

    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    expect(component.serverError()).toBeNull();
  }));

  it('should reject ftp URL', () => {
    component.form.controls.url.setValue('ftp://example.com/file');

    expect(component.form.valid).toBe(false);
  });

  it('should set isLoading to false after error', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should show recipe preview after successful extraction', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.extractedRecipe()).toEqual(mockRecipe);
  }));

  it('should show generic error on 502 response (streaming)', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(throwError(() => new Error('HTTP 502')));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should show generic error on 504 response (streaming)', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(throwError(() => new Error('HTTP 504')));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should show generic error on 404 response (streaming)', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(throwError(() => new Error('HTTP 404')));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should clear extracted recipe before new import', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);
    component.form.controls.url.setValue('https://example.com/other');
    component.onImport();

    expect(component.extractedRecipe()).toBeNull();

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
  }));

  it('should clear extracted recipe on error', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    recipeApiMock.importRecipeStream.mockReturnValue(
      throwError(() => new Error('Connection failed')),
    );
    component.form.controls.url.setValue('https://example.com/other');
    component.onImport();
    tick();

    expect(component.extractedRecipe()).toBeNull();
  }));

  it('should show generic error on 400 response (streaming)', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(throwError(() => new Error('HTTP 400')));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should show generic error on 422 response (streaming)', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(throwError(() => new Error('HTTP 422')));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
  }));

  it('should clear extracted recipe on discard', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.extractedRecipe()).toEqual(mockRecipe);

    component.onDiscardRecipe();

    expect(component.extractedRecipe()).toBeNull();
  }));

  it('should hide recipe preview after discard', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalled();
  }));

  it('should navigate to recipe detail on successful save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/123']);
  }));

  it('should show duplicate error on 409 response', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    // Manually set saveSuccess to simulate a previous save
    component.saveSuccess.set('Pasta Carbonara');

    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
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
    // Manually set saveSuccess to simulate a previous state
    component.saveSuccess.set('Pasta Carbonara');

    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    expect(component.saveSuccess()).toBeNull();

    tick();
  }));

  it('should navigate to recipe after save instead of showing success banner', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/123']);
  }));

  it('should render create recipe button', () => {
    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn).toBeTruthy();
    expect(btn.textContent).toContain('Create Recipe');
  });

  it('should show recipe preview when create button is clicked', () => {
    const btn = fixture.nativeElement.querySelector('.create-btn');
    btn.click();
    fixture.detectChanges();

    const preview = fixture.nativeElement.querySelector('yn-recipe-preview');
    expect(preview).toBeTruthy();
  });

  it('should set empty recipe template on create manually', () => {
    component.onCreateManually();

    const recipe = component.extractedRecipe();
    expect(recipe).toBeTruthy();
    expect(recipe!.title).toBe('');
    expect(recipe!.ingredients).toHaveLength(1);
    expect(recipe!.steps).toHaveLength(1);
  });

  it('should set isManualEntry on create manually', () => {
    component.onCreateManually();

    expect(component.isManualEntry()).toBe(true);
  });

  it('should navigate after saving manual recipe', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '456',
      title: 'My Recipe',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.onCreateManually();
    const recipe = component.extractedRecipe()!;
    component.onSaveRecipe({ ...recipe, title: 'My Recipe' });
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(
      expect.not.objectContaining({ sourceUrl: expect.anything() }),
    );
    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/456']);
  }));

  it('should navigate after successful save of manual entry', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '456',
      title: 'My Recipe',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    component.onCreateManually();
    component.onSaveRecipe(component.extractedRecipe()!);
    tick();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/456']);
  }));

  it('should reset isManualEntry on discard', () => {
    component.onCreateManually();
    expect(component.isManualEntry()).toBe(true);

    component.onDiscardRecipe();

    expect(component.isManualEntry()).toBe(false);
  });

  it('should disable create button while loading', () => {
    component.isLoading.set(true);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn.disabled).toBe(true);
  });

  it('should disable create button while saving', fakeAsync(() => {
    const subject = new Subject<SavedRecipeResponse>();
    recipeApiMock.saveRecipe.mockReturnValue(subject);

    component.onCreateManually();
    component.onSaveRecipe(component.extractedRecipe()!);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn.disabled).toBe(true);

    subject.next({
      identifier: '123',
      title: 'Test',
      createdAt: '2026-03-10T00:00:00Z',
    });
    subject.complete();
    tick();
  }));

  it('should clear sourceUrl when creating manually after import', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    expect(component.sourceUrl()).toBe('https://example.com/recipe');

    component.onCreateManually();

    expect(component.sourceUrl()).toBeNull();
  }));

  it('should reset isManualEntry when starting import', () => {
    component.onCreateManually();
    expect(component.isManualEntry()).toBe(true);

    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();

    expect(component.isManualEntry()).toBe(false);
  });

  it('should clear serverError and saveSuccess on create manually', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());
    recipeApiMock.saveRecipe.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    component.onSaveRecipe(mockRecipe);
    tick();
    expect(component.serverError()).toBe('dashboard.save.errors.generic');

    component.onCreateManually();

    expect(component.serverError()).toBeNull();
    expect(component.saveSuccess()).toBeNull();
  }));

  it('should disable create button when recipe preview is shown', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(successStream());

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn.disabled).toBe(true);
  }));

  it('should show generic error on fail stream event', fakeAsync(() => {
    recipeApiMock.importRecipeStream.mockReturnValue(
      of(
        { type: 'status' as const, data: 'Fetching page...' },
        { type: 'fail' as const, data: 'Server error' },
      ),
    );

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();
    tick();

    expect(component.serverError()).toBe('dashboard.import.errors.generic');
    expect(component.isLoading()).toBe(false);
  }));

  it('should update streamingStatus on status events', fakeAsync(() => {
    const subject = new Subject<ImportStreamEvent>();
    recipeApiMock.importRecipeStream.mockReturnValue(subject);

    component.form.controls.url.setValue('https://example.com/recipe');
    component.onImport();

    subject.next({ type: 'status', data: 'Fetching page...' });
    expect(component.streamingStatus()).toBe('Fetching page...');

    subject.next({ type: 'status', data: 'Extracting recipe...' });
    expect(component.streamingStatus()).toBe('Extracting recipe...');

    subject.next({ type: 'done', data: JSON.stringify(mockRecipe) });
    subject.complete();
    tick();

    expect(component.streamingStatus()).toBeNull();
  }));
});

describe('DashboardComponent – Share Intent', () => {
  let recipeApiMock: {
    importRecipe: ReturnType<typeof vi.fn>;
    importRecipeStream: ReturnType<typeof vi.fn>;
    saveRecipe: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    recipeApiMock = {
      importRecipe: vi.fn(),
      importRecipeStream: vi.fn().mockReturnValue(successStream()),
      saveRecipe: vi.fn(),
    };
    routerMock = { navigate: vi.fn() };
  });

  function createComponentWithQueryParams(queryParams: Record<string, string>) {
    const dashboardMock = {
      getSuggestions: vi.fn().mockReturnValue(of({ suggestions: [], quickActions: [] })),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };
    TestBed.configureTestingModule({
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
      providers: [
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: DashboardApiService, useValue: dashboardMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams } } },
      ],
    });

    const fixture = TestBed.createComponent(DashboardComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('should populate URL field from ?url query param on init', fakeAsync(() => {
    const { fixture, component } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    fixture.detectChanges();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should auto-start import when ?url is provided', fakeAsync(() => {
    const { fixture, component } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    fixture.detectChanges();
    tick();

    expect(component.extractedRecipe()).toEqual(mockRecipe);
  }));

  it('should extract URL from ?text query param', fakeAsync(() => {
    const { fixture, component } = createComponentWithQueryParams({
      text: 'Check this out https://example.com/recipe',
    });

    fixture.detectChanges();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should prefer ?url over ?text when both are present', fakeAsync(() => {
    const { fixture } = createComponentWithQueryParams({
      url: 'https://example.com/from-url',
      text: 'Check this https://example.com/from-text',
    });

    fixture.detectChanges();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/from-url');
  }));

  it('should not auto-import when no query params are present', () => {
    const { fixture } = createComponentWithQueryParams({});

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should not auto-import when ?text has no URL', () => {
    const { fixture } = createComponentWithQueryParams({
      text: 'Just some text without a link',
    });

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should extract http URL from ?text', fakeAsync(() => {
    const { fixture } = createComponentWithQueryParams({
      text: 'Try this http://example.com/recipe',
    });

    fixture.detectChanges();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('http://example.com/recipe');
  }));

  it('should extract first URL when ?text contains multiple URLs', fakeAsync(() => {
    const { fixture } = createComponentWithQueryParams({
      text: 'See https://first.com/recipe and https://second.com/recipe',
    });

    fixture.detectChanges();
    tick();

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://first.com/recipe');
  }));

  it('should not auto-import when ?text is empty string', () => {
    const { fixture } = createComponentWithQueryParams({ text: '' });

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });
});
