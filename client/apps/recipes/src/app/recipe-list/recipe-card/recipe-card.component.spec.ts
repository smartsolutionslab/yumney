import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { RecipeCardComponent } from './recipe-card.component';
import { setupTranslocoTesting } from '@yumney/shared/models';
import type { RecipeListItem } from '../../api';

const en = {
  recipes: {
    list: {
      assign: 'Assign to meal plan',
      servings: '{{count}} servings',
      prepTime: 'Prep {{minutes}} min',
      cookTime: 'Cook {{minutes}} min',
      multiSelect: {
        selectAriaLabel: 'Select recipe',
        deselectAriaLabel: 'Deselect recipe',
      },
    },
    favorite: {
      addAriaLabel: 'Add to favorites',
      removeAriaLabel: 'Remove from favorites',
    },
  },
};

const mockRecipe: RecipeListItem = {
  identifier: 'abc-123',
  title: 'Spaghetti Bolognese',
  description: 'A hearty Italian classic',
  servings: 4,
  prepTimeMinutes: 15,
  cookTimeMinutes: 30,
  difficulty: 'medium',
  imageUrl: 'https://example.com/spaghetti.jpg',
  createdAt: '2026-01-01T00:00:00Z',
  tags: ['italian', 'pasta'],
  isFavorite: false,
};

@Component({
  template: `
    <yn-recipe-card
      [recipe]="recipe()"
      [assignMode]="assignMode()"
      [multiSelectMode]="multiSelectMode()"
      [selected]="selected()"
      (toggleFavorite)="onToggleFavorite($event)"
      (assign)="onAssign($event)"
      (toggleSelect)="onToggleSelect($event)"
    />
  `,
  imports: [RecipeCardComponent],
})
class TestHostComponent {
  recipe = signal<RecipeListItem>(mockRecipe);
  assignMode = signal(false);
  multiSelectMode = signal(false);
  selected = signal(false);
  onToggleFavorite = vi.fn();
  onAssign = vi.fn();
  onToggleSelect = vi.fn();
}

describe('RecipeCardComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the recipe title', () => {
    const title = fixture.nativeElement.querySelector('.recipe-title');
    expect(title.textContent).toContain('Spaghetti Bolognese');
  });

  it('should show image when imageUrl is available', () => {
    const img = fixture.nativeElement.querySelector('img.recipe-image');
    expect(img).toBeTruthy();
    expect(img.src).toContain('https://example.com/spaghetti.jpg');
  });

  it('should show placeholder when no image is available', () => {
    host.recipe.set({ ...mockRecipe, imageUrl: null });
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('img.recipe-image');
    const placeholder = fixture.nativeElement.querySelector('.recipe-image-placeholder');
    expect(img).toBeNull();
    expect(placeholder).toBeTruthy();
  });

  it('should show description when available', () => {
    const description = fixture.nativeElement.querySelector('.recipe-description');
    expect(description.textContent).toContain('A hearty Italian classic');
  });

  it('should not show description when null', () => {
    host.recipe.set({ ...mockRecipe, description: null });
    fixture.detectChanges();

    const description = fixture.nativeElement.querySelector('.recipe-description');
    expect(description).toBeNull();
  });

  it('should emit toggleFavorite with recipe identifier when favorite button is toggled', () => {
    const favoriteButton = fixture.nativeElement.querySelector('yn-favorite-button button');
    favoriteButton.click();
    fixture.detectChanges();

    expect(host.onToggleFavorite).toHaveBeenCalledWith('abc-123');
  });

  it('should show assign overlay in assign mode', () => {
    host.assignMode.set(true);
    fixture.detectChanges();

    const overlay = fixture.nativeElement.querySelector('.assign-overlay');
    expect(overlay).toBeTruthy();
    expect(overlay.textContent).toContain('Assign to meal plan');
  });

  it('should not show favorite button in assign mode', () => {
    host.assignMode.set(true);
    fixture.detectChanges();

    const favoriteOverlay = fixture.nativeElement.querySelector('.favorite-overlay');
    expect(favoriteOverlay).toBeNull();
  });

  it('should emit assign with recipe when clicked in assign mode', () => {
    host.assignMode.set(true);
    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('.assign-card');
    card.click();
    fixture.detectChanges();

    expect(host.onAssign).toHaveBeenCalledWith(expect.objectContaining({ identifier: 'abc-123' }));
  });

  it('should render as a link in normal mode', () => {
    const link = fixture.nativeElement.querySelector('a.recipe-card');
    expect(link).toBeTruthy();
  });

  it('should render as a div in assign mode', () => {
    host.assignMode.set(true);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a.recipe-card');
    const div = fixture.nativeElement.querySelector('div.recipe-card');
    expect(link).toBeNull();
    expect(div).toBeTruthy();
  });

  it('should render as a button in multi-select mode', () => {
    host.multiSelectMode.set(true);
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a.recipe-card');
    const button = fixture.nativeElement.querySelector('button.multi-select-card');
    expect(link).toBeNull();
    expect(button).toBeTruthy();
  });

  it('should reflect selected state via aria-pressed and is-selected class', () => {
    host.multiSelectMode.set(true);
    host.selected.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.multi-select-card');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.classList.contains('is-selected')).toBe(true);
  });

  it('should show the check icon only when selected', () => {
    host.multiSelectMode.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.select-indicator i-lucide')).toBeNull();

    host.selected.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.select-indicator i-lucide')).toBeTruthy();
  });

  it('should emit toggleSelect with recipe identifier on click in multi-select mode', () => {
    host.multiSelectMode.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button.multi-select-card');
    button.click();
    fixture.detectChanges();

    expect(host.onToggleSelect).toHaveBeenCalledWith('abc-123');
  });
});
