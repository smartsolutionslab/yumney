import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting, type ScalableIngredient } from '@yumney/shared/models';
import { CreateShoppingListDialogComponent } from './create-shopping-list-dialog.component';

const en = {
  recipes: {
    detail: {
      createShoppingList: {
        title: 'Create shopping list',
        preview: '{{count}} ingredients for {{servings}} servings',
        previewNoServings: '{{count}} ingredients',
        confirm: 'Create list',
        creating: 'Creating...',
        cancel: 'Cancel',
      },
    },
  },
};

const ingredients: ScalableIngredient[] = [
  { name: 'Spaghetti', amount: 600, unit: 'g' },
  { name: 'Eggs', amount: 6, unit: null },
  { name: 'Parmesan', amount: null, unit: null },
];

describe('CreateShoppingListDialogComponent', () => {
  let fixture: ComponentFixture<CreateShoppingListDialogComponent>;
  let component: CreateShoppingListDialogComponent;

  function setup(isCreating = false) {
    TestBed.configureTestingModule({
      imports: [CreateShoppingListDialogComponent, setupTranslocoTesting(en)],
    });
    fixture = TestBed.createComponent(CreateShoppingListDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('recipeTitle', 'Pasta Carbonara');
    fixture.componentRef.setInput('desiredServings', 6);
    fixture.componentRef.setInput('ingredients', ingredients);
    fixture.componentRef.setInput('isCreating', isCreating);
    fixture.detectChanges();
  }

  it('should compose the suggested title from recipe title + desired servings', () => {
    setup();
    expect(component.suggestedTitle()).toBe('Pasta Carbonara (x6)');
  });

  it('should render every preview ingredient', () => {
    setup();
    const items = fixture.nativeElement.querySelectorAll('.preview-list li');
    expect(items.length).toBe(3);
    expect(items[0].textContent).toContain('600');
    expect(items[0].textContent).toContain('g');
    expect(items[0].textContent).toContain('Spaghetti');
    expect(items[2].textContent).toContain('Parmesan');
  });

  it('should emit confirmed when the confirm button is clicked', () => {
    setup();
    let emitted = 0;
    component.confirmed.subscribe(() => emitted++);

    fixture.nativeElement.querySelector('[data-testid="create-shopping-list-confirm"]').click();

    expect(emitted).toBe(1);
  });

  it('should emit cancelled when the cancel button is clicked', () => {
    setup();
    let emitted = 0;
    component.cancelled.subscribe(() => emitted++);

    fixture.nativeElement.querySelector('[data-testid="create-shopping-list-cancel"]').click();

    expect(emitted).toBe(1);
  });

  it('should disable buttons and show creating label while in flight', () => {
    setup(true);

    const confirm = fixture.nativeElement.querySelector('[data-testid="create-shopping-list-confirm"]');
    const cancel = fixture.nativeElement.querySelector('[data-testid="create-shopping-list-cancel"]');
    expect(confirm.disabled).toBe(true);
    expect(cancel.disabled).toBe(true);
    expect(confirm.textContent).toContain('Creating...');
  });

  it('should ignore Escape while a request is in flight', () => {
    setup(true);
    let emitted = 0;
    component.cancelled.subscribe(() => emitted++);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(emitted).toBe(0);
  });

  it('should emit cancelled on Escape when idle', () => {
    setup();
    let emitted = 0;
    component.cancelled.subscribe(() => emitted++);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

    expect(emitted).toBe(1);
  });

  it('should emit cancelled when the overlay is clicked', () => {
    setup();
    let emitted = 0;
    component.cancelled.subscribe(() => emitted++);

    const overlay = fixture.nativeElement.querySelector('.yn-dialog-overlay');
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(emitted).toBe(1);
  });

  it('should ignore overlay clicks while a request is in flight', () => {
    setup(true);
    let emitted = 0;
    component.cancelled.subscribe(() => emitted++);

    const overlay = fixture.nativeElement.querySelector('.yn-dialog-overlay');
    overlay.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(emitted).toBe(0);
  });

  it('should drop the servings suffix when desiredServings is null', () => {
    TestBed.configureTestingModule({
      imports: [CreateShoppingListDialogComponent, setupTranslocoTesting(en)],
    });
    fixture = TestBed.createComponent(CreateShoppingListDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('recipeTitle', 'Pasta Carbonara');
    fixture.componentRef.setInput('desiredServings', null);
    fixture.componentRef.setInput('ingredients', ingredients);
    fixture.componentRef.setInput('isCreating', false);
    fixture.detectChanges();

    expect(component.suggestedTitle()).toBe('Pasta Carbonara');
    const subtitle = fixture.nativeElement.querySelector('[data-testid="create-shopping-list-suggested-title"]');
    expect(subtitle.textContent.trim()).toBe('Pasta Carbonara');
    const heading = fixture.nativeElement.querySelector('.preview-heading');
    expect(heading.textContent).toContain('3 ingredients');
    expect(heading.textContent).not.toContain('servings');
  });
});
