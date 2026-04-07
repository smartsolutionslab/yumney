import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, viewChild } from '@angular/core';
import { ImportRecipeResponse } from '@yumney/shared/api-client';
import { RecipePreviewComponent } from './recipe-preview.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

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
  shared: {
    editableList: {
      moveUp: 'Move up',
      moveDown: 'Move down',
      remove: 'Remove',
    },
  },
  dashboard: {
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

@Component({
  template: `
    <yn-recipe-preview
      [recipe]="recipe"
      [previewTitle]="previewTitle"
      (save)="onSave($event)"
      (discard)="onDiscard()"
    />
  `,
  imports: [RecipePreviewComponent],
})
class TestHostComponent {
  recipe = mockRecipe;
  previewTitle: string | undefined;
  onSave = vi.fn();
  onDiscard = vi.fn();
  preview = viewChild(RecipePreviewComponent);
}

describe('RecipePreviewComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;
  let preview: RecipePreviewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        TestHostComponent,
        setupTranslocoTesting(en),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    preview = host.preview() as RecipePreviewComponent;
  });

  it('should create the component', () => {
    expect(host.preview()).toBeTruthy();
  });

  it('should render the preview title', () => {
    const heading = fixture.nativeElement.querySelector('h2');
    expect(heading.textContent).toContain('Review Extracted Recipe');
  });

  it('should populate the title field from recipe input', () => {
    const input = fixture.nativeElement.querySelector('#preview-title') as HTMLInputElement;
    expect(input.value).toBe('Pasta Carbonara');
  });

  it('should populate the description field', () => {
    const textarea = fixture.nativeElement.querySelector(
      '#preview-description',
    ) as HTMLTextAreaElement;
    expect(textarea.value).toBe('A classic Italian pasta dish');
  });

  it('should populate servings field', () => {
    const input = fixture.nativeElement.querySelector('#preview-servings') as HTMLInputElement;
    expect(input.value).toBe('4');
  });

  it('should render all ingredients', () => {
    const items = fixture.nativeElement.querySelectorAll('.ingredient-fields');
    expect(items.length).toBe(2);
  });

  it('should render all steps', () => {
    const items = fixture.nativeElement.querySelectorAll('.step-fields');
    expect(items.length).toBe(2);
  });

  it('should add a new ingredient when add button is clicked', () => {
    const addBtn = fixture.nativeElement.querySelectorAll('.add-btn')[0];
    addBtn.click();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.ingredient-fields');
    expect(items.length).toBe(3);
  });

  it('should remove an ingredient', () => {
    const removeBtn = fixture.nativeElement.querySelector(
      '[formarrayname="ingredients"] [aria-label="Remove"]',
    );
    removeBtn.click();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.ingredient-fields');
    expect(items.length).toBe(1);
  });

  it('should add a new step when add button is clicked', () => {
    const addBtns = fixture.nativeElement.querySelectorAll('.add-btn');
    const addStepBtn = addBtns[addBtns.length - 1];
    addStepBtn.click();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.step-fields');
    expect(items.length).toBe(3);
  });

  it('should remove a step', () => {
    const removeBtn = fixture.nativeElement.querySelector(
      '[formarrayname="steps"] [aria-label="Remove"]',
    );
    removeBtn.click();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.step-fields');
    expect(items.length).toBe(1);
  });

  it('should emit save with edited recipe on valid submit', () => {
    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Pasta Carbonara',
        servings: 4,
        ingredients: expect.arrayContaining([expect.objectContaining({ name: 'Spaghetti' })]),
      }),
    );
  });

  it('should emit discard when discard button is clicked', () => {
    const discardBtn = fixture.nativeElement.querySelector('.discard-btn');
    discardBtn.click();

    expect(host.onDiscard).toHaveBeenCalled();
  });

  it('should not emit save when title is empty', () => {
    preview.form.controls.title.setValue('');
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.onSave).not.toHaveBeenCalled();
  });

  it('should show title required error when submitting with empty title', () => {
    preview.form.controls.title.setValue('');
    preview.onSave();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.metadata-section .field-error');
    expect(error.textContent).toContain('Title is required.');
  });

  it('should show title max length error', () => {
    preview.form.controls.title.setValue('a'.repeat(201));
    preview.onSave();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.metadata-section .field-error');
    expect(error.textContent).toContain('Title must not exceed 200 characters.');
  });

  it('should show ingredient name required error', () => {
    preview.addIngredient();
    preview.onSave();
    fixture.detectChanges();

    const errors = fixture.nativeElement.querySelectorAll('.ingredient-fields .field-error');
    expect(errors.length).toBeGreaterThan(0);
    expect(errors[errors.length - 1].textContent).toContain('Ingredient name is required.');
  });

  it('should show step description required error', () => {
    preview.addStep();
    preview.onSave();
    fixture.detectChanges();

    const errors = fixture.nativeElement.querySelectorAll('.step-fields .field-error');
    expect(errors.length).toBeGreaterThan(0);
    expect(errors[errors.length - 1].textContent).toContain('Step description is required.');
  });

  it('should preserve imageUrl from original recipe in save output', () => {
    host.recipe = { ...mockRecipe, imageUrl: 'https://example.com/image.jpg' };
    fixture.detectChanges();

    // Re-create to pick up new recipe
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    host.recipe = { ...mockRecipe, imageUrl: 'https://example.com/image.jpg' };
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.onSave).toHaveBeenCalledWith(
      expect.objectContaining({ imageUrl: 'https://example.com/image.jpg' }),
    );
  });

  it('should reorder ingredients up', () => {
    preview.moveIngredientUp(1);
    fixture.detectChanges();

    const nameInputs = fixture.nativeElement.querySelectorAll(
      '.ingredient-fields input[formcontrolname="name"]',
    );
    expect(nameInputs[0].value).toBe('Pancetta');
    expect(nameInputs[1].value).toBe('Spaghetti');
  });

  it('should reorder ingredients down', () => {
    preview.moveIngredientDown(0);
    fixture.detectChanges();

    const nameInputs = fixture.nativeElement.querySelectorAll(
      '.ingredient-fields input[formcontrolname="name"]',
    );
    expect(nameInputs[0].value).toBe('Pancetta');
    expect(nameInputs[1].value).toBe('Spaghetti');
  });

  it('should reorder steps up', () => {
    preview.moveStepUp(1);
    fixture.detectChanges();

    const textareas = fixture.nativeElement.querySelectorAll(
      '.step-fields textarea[formcontrolname="description"]',
    );
    expect(textareas[0].value).toBe('Fry pancetta');
    expect(textareas[1].value).toBe('Cook pasta');
  });

  it('should reorder steps down', () => {
    preview.moveStepDown(0);
    fixture.detectChanges();

    const textareas = fixture.nativeElement.querySelectorAll(
      '.step-fields textarea[formcontrolname="description"]',
    );
    expect(textareas[0].value).toBe('Fry pancetta');
    expect(textareas[1].value).toBe('Cook pasta');
  });

  it('should number steps sequentially in save output', () => {
    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const savedRecipe = host.onSave.mock.calls[0][0] as ImportRecipeResponse;
    expect(savedRecipe.steps[0].number).toBe(1);
    expect(savedRecipe.steps[1].number).toBe(2);
  });

  it('should not render image section when imageUrl is null', () => {
    const img = fixture.nativeElement.querySelector('.preview-image');
    expect(img).toBeNull();
  });

  it('should populate prepTime and cookTime fields', () => {
    const prepInput = fixture.nativeElement.querySelector('#preview-prepTime') as HTMLInputElement;
    const cookInput = fixture.nativeElement.querySelector('#preview-cookTime') as HTMLInputElement;
    expect(prepInput.value).toBe('10');
    expect(cookInput.value).toBe('20');
  });

  it('should populate difficulty field', () => {
    const input = fixture.nativeElement.querySelector('#preview-difficulty') as HTMLInputElement;
    expect(input.value).toBe('medium');
  });

  it('should emit edited values on save', () => {
    preview.form.controls.title.setValue('Updated Title');
    preview.form.controls.servings.setValue(2);

    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Updated Title',
        servings: 2,
      }),
    );
  });

  it('should handle recipe with null optional fields', () => {
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    host.recipe = {
      ...mockRecipe,
      description: null,
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
    };
    fixture.detectChanges();

    const textarea = fixture.nativeElement.querySelector(
      '#preview-description',
    ) as HTMLTextAreaElement;
    expect(textarea.value).toBe('');

    const form = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(host.onSave).toHaveBeenCalledWith(
      expect.objectContaining({
        description: null,
        servings: null,
        difficulty: null,
      }),
    );
  });

  it('should render image section when imageUrl is set', () => {
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    host.recipe = { ...mockRecipe, imageUrl: 'https://example.com/image.jpg' };
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('.preview-image img');
    expect(img).toBeTruthy();
    expect(img.src).toContain('https://example.com/image.jpg');
  });

  it('should render custom previewTitle when provided', () => {
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    host.previewTitle = 'New Recipe';
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('h2');
    expect(heading.textContent).toContain('New Recipe');
  });

  it('should render default title when previewTitle is not provided', () => {
    const heading = fixture.nativeElement.querySelector('h2');
    expect(heading.textContent).toContain('Review Extracted Recipe');
  });

  it('should render empty recipe correctly', () => {
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    host.recipe = {
      title: '',
      description: null,
      ingredients: [{ name: '', amount: null, unit: null }],
      steps: [{ number: 1, description: '' }],
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
      imageUrl: null,
    };
    fixture.detectChanges();

    const titleInput = fixture.nativeElement.querySelector('#preview-title') as HTMLInputElement;
    expect(titleInput.value).toBe('');

    const ingredients = fixture.nativeElement.querySelectorAll('.ingredient-fields');
    expect(ingredients.length).toBe(1);

    const steps = fixture.nativeElement.querySelectorAll('.step-fields');
    expect(steps.length).toBe(1);
  });
});
