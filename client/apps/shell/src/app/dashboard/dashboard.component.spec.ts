import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { ImportPanelComponent } from '../integrations/recipes/import-panel/import-panel.component';
import { setupTranslocoTesting, UI } from '@yumney/shared/models';
import { DashboardApiService } from '@yumney/shared/api-dashboard';
import { RecipeApiService, ImportRecipeResponse, SavedRecipeResponse } from '@yumney/shared/api-recipes';

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
      sectionTitle: 'Import a Recipe',
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
    photoImport: {
      title: 'Scan a Recipe',
      subtitle: 'Take a photo of a recipe',
      submit: 'Choose Photos',
      scanRecipe: 'Scan recipe',
      scanIngredients: 'Scan ingredients',
      extracting: 'Extracting...',
      hint: 'Best results with one recipe per photo',
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
      importAnother: 'Import Another',
      errors: {
        duplicate: 'This recipe has already been imported.',
        generic: 'Failed to save recipe. Please try again.',
      },
    },
    share: {
      noUrlFound: 'No URL found in shared text.',
    },
    suggestions: { title: 'Suggested for you' },
    recentActivity: { title: 'Recent activity', empty: 'No recent activity' },
  },
};

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let importPanel: ImportPanelComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let recipeApiMock: {
    importRecipe: ReturnType<typeof vi.fn>;
    importRecipeStream: ReturnType<typeof vi.fn>;
    importFromPhotos: ReturnType<typeof vi.fn>;
    saveRecipe: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };
  let activatedRouteMock: {
    snapshot: { queryParams: Record<string, string> };
    queryParams: import('rxjs').Observable<Record<string, string>>;
  };

  beforeEach(async () => {
    recipeApiMock = {
      importRecipe: vi.fn(),
      importRecipeStream: vi.fn(),
      importFromPhotos: vi.fn(),
      saveRecipe: vi.fn(),
    };
    routerMock = { navigate: vi.fn() };
    activatedRouteMock = { snapshot: { queryParams: {} }, queryParams: of({}) };

    const dashboardApiMock = {
      getSuggestions: vi.fn().mockReturnValue(of({ suggestions: [], quickActions: [] })),
      getRecentActivity: vi.fn().mockReturnValue(of([])),
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: DashboardApiService, useValue: dashboardApiMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    importPanel = fixture.debugElement.query(By.directive(ImportPanelComponent)).componentInstance as ImportPanelComponent;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render the welcome message', () => {
    expect(fixture.nativeElement.textContent).toContain('Welcome to Yumney!');
  });

  it('should render the dashboard title', () => {
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading.textContent).toContain('Dashboard');
  });

  it('should render the URL import child component when section expanded', () => {
    expect(fixture.nativeElement.querySelector('yn-url-import')).toBeTruthy();
  });

  // ── Extracted recipe orchestration ─────────────────────────────────────────

  it('should store extracted recipe and sourceUrl when child emits extracted', () => {
    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });

    expect(importPanel.extractedRecipe()).toEqual(mockRecipe);
    expect(importPanel.sourceUrl()).toBe('https://example.com/recipe');
  });

  it('should show recipe preview after extraction', () => {
    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeTruthy();
  });

  it('should not show recipe preview when no recipe is extracted', () => {
    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeNull();
  });

  it('should set serverError when child emits failed', () => {
    importPanel.onUrlImportFailed('dashboard.import.errors.generic');

    expect(importPanel.serverError()).toBe('dashboard.import.errors.generic');
  });

  it('should clear stale state when child emits importStarted', () => {
    importPanel.extractedRecipe.set(mockRecipe);
    importPanel.serverError.set('dashboard.import.errors.generic');
    importPanel.saveSuccess.set('Pasta');
    importPanel.isManualEntry.set(true);

    importPanel.onUrlImportStarted();

    expect(importPanel.extractedRecipe()).toBeNull();
    expect(importPanel.serverError()).toBeNull();
    expect(importPanel.saveSuccess()).toBeNull();
    expect(importPanel.isManualEntry()).toBe(false);
  });

  // ── Save flow ──────────────────────────────────────────────────────────────

  it('should call saveRecipe API on save', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalled();
  }));

  it('should show success banner with the saved title before navigating', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(importPanel.saveSuccess()).toBe('Pasta Carbonara');
    expect(routerMock.navigate).not.toHaveBeenCalled();

    tick(UI.SAVED_INDICATOR_MS);
  }));

  it('should navigate to recipe detail after the success-banner delay', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick(UI.SAVED_INDICATOR_MS);

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/123']);
  }));

  it('should cancel the pending navigate when the user clicks Import another', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    importPanel.onDiscardRecipe();
    tick(UI.SAVED_INDICATOR_MS);

    expect(routerMock.navigate).not.toHaveBeenCalled();
    expect(importPanel.saveSuccess()).toBeNull();
  }));

  it('should show duplicate error on 409 response', fakeAsync(() => {
    recipeApiMock.saveRecipe.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(importPanel.serverError()).toBe('dashboard.save.errors.duplicate');
  }));

  it('should show generic save error on 500 response', fakeAsync(() => {
    recipeApiMock.saveRecipe.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(importPanel.serverError()).toBe('dashboard.save.errors.generic');
  }));

  it('should set isSaving during save operation', fakeAsync(() => {
    const subject = new Subject<SavedRecipeResponse>();
    recipeApiMock.saveRecipe.mockReturnValue(subject);

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    expect(importPanel.isSaving()).toBe(true);

    subject.next({
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    });
    subject.complete();
    tick();

    expect(importPanel.isSaving()).toBe(false);
  }));

  it('should call saveRecipe without sourceUrl when sourceUrl is null', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.extractedRecipe.set(mockRecipe);
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(expect.not.objectContaining({ sourceUrl: expect.anything() }));
  }));

  it('should include sourceUrl in save request', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(expect.objectContaining({ sourceUrl: 'https://example.com/recipe' }));
  }));

  it('should map recipe fields to save request correctly', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '123',
      title: 'Pasta Carbonara',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
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
    recipeApiMock.saveRecipe.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);
    tick();

    expect(importPanel.isSaving()).toBe(false);
  }));

  it('should clear serverError when starting a save', fakeAsync(() => {
    importPanel.serverError.set('previous error');
    recipeApiMock.saveRecipe.mockReturnValue(of({ identifier: '1', title: 'X', createdAt: '2026-03-10T00:00:00Z' } as SavedRecipeResponse));

    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    importPanel.onSaveRecipe(mockRecipe);

    expect(importPanel.serverError()).toBeNull();
    tick();
  }));

  // ── Discard flow ───────────────────────────────────────────────────────────

  it('should clear extracted recipe on discard', () => {
    importPanel.extractedRecipe.set(mockRecipe);

    importPanel.onDiscardRecipe();

    expect(importPanel.extractedRecipe()).toBeNull();
  });

  it('should hide recipe preview after discard', () => {
    importPanel.extractedRecipe.set(mockRecipe);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeTruthy();

    importPanel.onDiscardRecipe();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeNull();
  });

  it('should clear serverError on discard', () => {
    importPanel.serverError.set('dashboard.save.errors.generic');

    importPanel.onDiscardRecipe();

    expect(importPanel.serverError()).toBeNull();
  });

  it('should reset isManualEntry on discard', () => {
    importPanel.onCreateManually();
    expect(importPanel.isManualEntry()).toBe(true);

    importPanel.onDiscardRecipe();

    expect(importPanel.isManualEntry()).toBe(false);
  });

  // ── Manual create ──────────────────────────────────────────────────────────

  it('should render create recipe button', () => {
    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn).toBeTruthy();
    expect(btn.textContent).toContain('Create Recipe');
  });

  it('should show recipe preview when create button is clicked', () => {
    const btn = fixture.nativeElement.querySelector('.create-btn');
    btn.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('yn-recipe-preview')).toBeTruthy();
  });

  it('should set empty recipe template on create manually', () => {
    importPanel.onCreateManually();

    const recipe = importPanel.extractedRecipe();
    if (!recipe) throw new Error('extractedRecipe should be set after onCreateManually');
    expect(recipe.title).toBe('');
    expect(recipe.ingredients).toHaveLength(1);
    expect(recipe.steps).toHaveLength(1);
  });

  it('should set isManualEntry on create manually', () => {
    importPanel.onCreateManually();

    expect(importPanel.isManualEntry()).toBe(true);
  });

  it('should navigate after saving manual recipe', fakeAsync(() => {
    const savedResponse: SavedRecipeResponse = {
      identifier: '456',
      title: 'My Recipe',
      createdAt: '2026-03-10T00:00:00Z',
    };
    recipeApiMock.saveRecipe.mockReturnValue(of(savedResponse));

    importPanel.onCreateManually();
    const draft = importPanel.extractedRecipe();
    if (!draft) throw new Error('extractedRecipe should be set after onCreateManually');
    importPanel.onSaveRecipe({ ...draft, title: 'My Recipe' });
    tick(UI.SAVED_INDICATOR_MS);

    expect(recipeApiMock.saveRecipe).toHaveBeenCalledWith(expect.not.objectContaining({ sourceUrl: expect.anything() }));
    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes/456']);
  }));

  it('should disable create button while loading', () => {
    importPanel.isLoading.set(true);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn.disabled).toBe(true);
  });

  it('should disable create button while saving', fakeAsync(() => {
    const subject = new Subject<SavedRecipeResponse>();
    recipeApiMock.saveRecipe.mockReturnValue(subject);

    importPanel.onCreateManually();
    const draft = importPanel.extractedRecipe();
    if (!draft) throw new Error('extractedRecipe should be set after onCreateManually');
    importPanel.onSaveRecipe(draft);
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

  it('should clear sourceUrl when creating manually after import', () => {
    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    expect(importPanel.sourceUrl()).toBe('https://example.com/recipe');

    importPanel.onCreateManually();

    expect(importPanel.sourceUrl()).toBeNull();
  });

  it('should clear serverError and saveSuccess on create manually', () => {
    importPanel.serverError.set('dashboard.save.errors.generic');
    importPanel.saveSuccess.set('Pasta');

    importPanel.onCreateManually();

    expect(importPanel.serverError()).toBeNull();
    expect(importPanel.saveSuccess()).toBeNull();
  });

  it('should disable create button when recipe preview is shown', () => {
    importPanel.onUrlExtracted({ recipe: mockRecipe, sourceUrl: 'https://example.com/recipe' });
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('.create-btn');
    expect(btn.disabled).toBe(true);
  });

  it('should navigate to /recipes on cook_now quick action', () => {
    component.onQuickAction('cook_now');

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes']);
  });

  it('should navigate to /recipes with multiSelect on meal_prep quick action', () => {
    component.onQuickAction('meal_prep');

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes'], {
      queryParams: { multiSelect: 'true' },
    });
  });

  it('should preselect suggested recipe ids when meal_prep fires with suggestions', () => {
    component.suggestions.set({
      suggestions: [
        { recipeIdentifier: 'abc', title: 'A', imageUrl: null, prepTimeMinutes: null, reason: '' },
        { recipeIdentifier: 'def', title: 'B', imageUrl: null, prepTimeMinutes: null, reason: '' },
      ],
      quickActions: ['meal_prep'],
    });

    component.onQuickAction('meal_prep');

    expect(routerMock.navigate).toHaveBeenCalledWith(['/recipes'], {
      queryParams: { multiSelect: 'true', preselect: 'abc,def' },
    });
  });

  it('should expand the import section for any other quick action', () => {
    component.onQuickAction('try_something_new');

    expect(importPanel.importSectionExpanded()).toBe(true);
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });
});

describe('DashboardComponent – Share Intent', () => {
  let recipeApiMock: {
    importRecipe: ReturnType<typeof vi.fn>;
    importRecipeStream: ReturnType<typeof vi.fn>;
    importFromPhotos: ReturnType<typeof vi.fn>;
    saveRecipe: ReturnType<typeof vi.fn>;
  };
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    recipeApiMock = {
      importRecipe: vi.fn(),
      importRecipeStream: vi.fn().mockReturnValue(of({ type: 'done' as const, data: JSON.stringify(mockRecipe) })),
      importFromPhotos: vi.fn(),
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
      imports: [DashboardComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: DashboardApiService, useValue: dashboardMock },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams }, queryParams: of(queryParams) },
        },
      ],
    });

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    const importPanelEl = fixture.debugElement.query(By.directive(ImportPanelComponent));
    return { fixture, component: fixture.componentInstance, importPanel: importPanelEl?.componentInstance as ImportPanelComponent };
  }

  /**
   * The share-intent flow defers the actual import to a microtask after the
   * import section expands and the UrlImportComponent renders. Tests must
   * detectChanges twice (initial render → expand → child renders) and flush
   * the microtask queue before the API spy is hit.
   */
  function runShareFlow(fixture: ComponentFixture<DashboardComponent>): void {
    fixture.detectChanges(); // initial render + queryParams subscription
    tick(); // flush microtask that calls urlImport()?.importUrl(...)
    fixture.detectChanges(); // re-render after extraction
    tick();
  }

  it('should auto-start import when ?url query param is provided', fakeAsync(() => {
    const { fixture, importPanel } = createComponentWithQueryParams({ url: 'https://example.com/recipe' });

    runShareFlow(fixture);

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should set extractedRecipe after share-import succeeds', fakeAsync(() => {
    const { fixture, component, importPanel } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    runShareFlow(fixture);

    expect(importPanel.extractedRecipe()).toEqual(mockRecipe);
  }));

  it('should extract URL from ?text query param', fakeAsync(() => {
    const { fixture, importPanel } = createComponentWithQueryParams({
      text: 'Check this out https://example.com/recipe',
    });

    runShareFlow(fixture);

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/recipe');
  }));

  it('should prefer ?url over ?text when both are present', fakeAsync(() => {
    const { fixture, importPanel } = createComponentWithQueryParams({
      url: 'https://example.com/from-url',
      text: 'Check this https://example.com/from-text',
    });

    runShareFlow(fixture);

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://example.com/from-url');
  }));

  it('should not auto-import when no query params are present', () => {
    const { fixture, importPanel } = createComponentWithQueryParams({});

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should not auto-import when ?text has no URL', () => {
    const { fixture, importPanel } = createComponentWithQueryParams({
      text: 'Just some text without a link',
    });

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should extract http URL from ?text', fakeAsync(() => {
    const { fixture, importPanel } = createComponentWithQueryParams({
      text: 'Try this http://example.com/recipe',
    });

    runShareFlow(fixture);

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('http://example.com/recipe');
  }));

  it('should extract first URL when ?text contains multiple URLs', fakeAsync(() => {
    const { fixture, importPanel } = createComponentWithQueryParams({
      text: 'See https://first.com/recipe and https://second.com/recipe',
    });

    runShareFlow(fixture);

    expect(recipeApiMock.importRecipeStream).toHaveBeenCalledWith('https://first.com/recipe');
  }));

  it('should not auto-import when ?text is empty string', () => {
    const { fixture, importPanel } = createComponentWithQueryParams({ text: '' });

    fixture.detectChanges();

    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should show share toast when URL is shared', fakeAsync(() => {
    const { fixture, component, importPanel } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    runShareFlow(fixture);

    expect(component.shareToast()).toBe('https://example.com/recipe');
  }));

  it('should expand import section when URL is shared', fakeAsync(() => {
    const { fixture, component, importPanel } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    runShareFlow(fixture);

    expect(importPanel.importSectionExpanded()).toBe(true);
  }));

  it('should show error when shared text contains no URL', () => {
    const { fixture, component, importPanel } = createComponentWithQueryParams({
      text: 'Just some text without a link',
    });

    fixture.detectChanges();

    expect(component.serverError()).toBe('dashboard.share.noUrlFound');
    expect(importPanel.importSectionExpanded()).toBe(true);
    expect(recipeApiMock.importRecipeStream).not.toHaveBeenCalled();
  });

  it('should dismiss share toast when dismissShareToast is called', fakeAsync(() => {
    const { fixture, component, importPanel } = createComponentWithQueryParams({
      url: 'https://example.com/recipe',
    });

    runShareFlow(fixture);
    expect(component.shareToast()).toBeTruthy();

    component.dismissShareToast();

    expect(component.shareToast()).toBeNull();
  }));
});
