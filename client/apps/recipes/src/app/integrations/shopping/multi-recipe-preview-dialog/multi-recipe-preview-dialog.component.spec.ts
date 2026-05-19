import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { MultiRecipePreviewDialogComponent, type MultiRecipeSelection } from './multi-recipe-preview-dialog.component';

const en = {
  recipes: {
    list: {
      multiSelect: {
        preview: {
          title: 'Review shopping list',
          subtitle: '{{count}} recipes selected',
          loading: 'Loading recipe details...',
          fallbackTitle: 'Recipe',
          recipesHeading: 'Recipes & servings',
          mergedHeading: '{{count}} merged ingredients',
          servings: '{{count}} servings',
          decreaseAriaLabel: 'Decrease servings for {{title}}',
          increaseAriaLabel: 'Increase servings for {{title}}',
          confirm: 'Create list',
          creating: 'Creating...',
          cancel: 'Cancel',
        },
      },
    },
  },
};

const sampleRecipes: MultiRecipeSelection[] = [
  {
    identifier: 'abc-123',
    title: 'Pasta',
    originalServings: 4,
    desiredServings: 4,
    ingredients: [
      { name: 'Flour', amount: 200, unit: 'g' },
      { name: 'Eggs', amount: 2, unit: null },
    ],
  },
  {
    identifier: 'def-456',
    title: 'Cake',
    originalServings: 4,
    desiredServings: 4,
    ingredients: [
      { name: 'Flour', amount: 300, unit: 'g' },
      { name: 'Milk', amount: 250, unit: 'ml' },
    ],
  },
];

describe('MultiRecipePreviewDialogComponent', () => {
  let fixture: ComponentFixture<MultiRecipePreviewDialogComponent>;
  let component: MultiRecipePreviewDialogComponent;

  function setup(
    overrides: Partial<{
      recipes: MultiRecipeSelection[];
      isLoading: boolean;
      isCreating: boolean;
    }> = {},
  ) {
    TestBed.configureTestingModule({
      imports: [MultiRecipePreviewDialogComponent, setupTranslocoTesting(en)],
    });
    fixture = TestBed.createComponent(MultiRecipePreviewDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('recipes', overrides.recipes ?? sampleRecipes);
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);
    fixture.componentRef.setInput('isCreating', overrides.isCreating ?? false);
    fixture.detectChanges();
  }

  it('should render a row per recipe with the current desired servings', () => {
    setup();
    const rows = fixture.nativeElement.querySelectorAll('.recipe-row');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Pasta');
    expect(rows[0].textContent).toContain('4 servings');
  });

  it('should compute merged ingredients with case-insensitive name+unit matching', () => {
    setup();
    const merged = component.mergedIngredients();
    expect(merged).toHaveLength(3);
    expect(merged.find((entry) => entry.name === 'Flour')?.amount).toBe(500);
    expect(merged.find((entry) => entry.name === 'Eggs')?.amount).toBe(2);
    expect(merged.find((entry) => entry.name === 'Milk')?.amount).toBe(250);
  });

  it('should show a loading state and skip the merged section', () => {
    setup({ isLoading: true });
    expect(fixture.nativeElement.querySelector('.dialog-loading')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-merged"]')).toBeNull();
    expect(component.mergedIngredients()).toHaveLength(0);
  });

  it('should emit servingsChanged on increase / decrease', () => {
    setup();
    let lastChange: { identifier: string; servings: number } | null = null;
    component.servingsChanged.subscribe((event) => (lastChange = event));

    component.onIncrease('abc-123');
    expect(lastChange).toEqual({ identifier: 'abc-123', servings: 5 });

    component.onDecrease('abc-123');
    expect(lastChange).toEqual({ identifier: 'abc-123', servings: 3 });
  });

  it('should not emit a decrease below 1', () => {
    const recipes: MultiRecipeSelection[] = [{ ...sampleRecipes[0], desiredServings: 1 }];
    setup({ recipes });
    let count = 0;
    component.servingsChanged.subscribe(() => count++);

    component.onDecrease('abc-123');

    expect(count).toBe(0);
  });

  it('should emit confirmed when the confirm button is clicked', () => {
    setup();
    let count = 0;
    component.confirmed.subscribe(() => count++);

    fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-confirm"]').click();

    expect(count).toBe(1);
  });

  it('should emit cancelled when the cancel button is clicked', () => {
    setup();
    let count = 0;
    component.cancelled.subscribe(() => count++);

    fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-cancel"]').click();

    expect(count).toBe(1);
  });

  it('should disable the confirm button while creating', () => {
    setup({ isCreating: true });
    const confirm = fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-confirm"]');
    expect(confirm.disabled).toBe(true);
    expect(confirm.textContent).toContain('Creating...');
  });

  it('should disable the confirm button while loading', () => {
    setup({ isLoading: true });
    const confirm = fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-confirm"]');
    expect(confirm.disabled).toBe(true);
  });

  it('should disable the confirm button when there are no merged ingredients', () => {
    setup({ recipes: [{ ...sampleRecipes[0], ingredients: [] }] });
    const confirm = fixture.nativeElement.querySelector('[data-testid="multi-recipe-preview-confirm"]');
    expect(confirm.disabled).toBe(true);
  });

  it('should ignore Escape while creating', () => {
    setup({ isCreating: true });
    let count = 0;
    component.cancelled.subscribe(() => count++);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(count).toBe(0);
  });

  it('should emit cancelled on Escape when idle', () => {
    setup();
    let count = 0;
    component.cancelled.subscribe(() => count++);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(count).toBe(1);
  });

  it('should emit cancelled on overlay click', () => {
    setup();
    let count = 0;
    component.cancelled.subscribe(() => count++);

    fixture.nativeElement.querySelector('.yn-dialog-overlay').dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(count).toBe(1);
  });
});
