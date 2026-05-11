import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideYumneyIcons } from '@yumney/ui';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { PantryComponent } from './pantry.component';
import { ShoppingApiService, type IngredientBalance } from '../api';

const en = {
  shopping: {
    pantry: {
      title: 'Pantry',
      subtitle: "What's at home right now",
      staple: 'Staple',
      daysSinceBought: 'Bought {{count}}d ago',
      freshness: {
        fresh: 'Fresh',
        'use-soon': 'Use soon',
        'check-it': 'Check it',
      },
      freeze: {
        action: 'I froze it',
        ariaLabel: 'Mark {{name}} as frozen',
      },
      empty: {
        title: 'Nothing in your pantry yet',
        message: 'Add items.',
      },
      errors: {
        loadFailed: 'Pantry could not be loaded.',
        freezeFailed: 'Could not mark item as frozen.',
      },
    },
    category: {
      produce: 'Produce',
      dairy: 'Dairy',
      'meat-fish': 'Meat & Fish',
      pantry: 'Pantry',
      other: 'Other',
    },
  },
};

const fullResponse: IngredientBalance = {
  items: [
    {
      itemName: 'Chicken breast',
      quantity: 500,
      unit: 'g',
      category: 'meat-fish',
      source: 'AtHome',
      freshness: 'UseSoon',
      daysSinceBought: 1,
    },
    {
      itemName: 'Salt',
      quantity: null,
      unit: null,
      category: 'pantry',
      source: 'Staple',
      freshness: 'NotTracked',
      daysSinceBought: null,
    },
    {
      itemName: 'Spinach',
      quantity: 200,
      unit: 'g',
      category: 'produce',
      source: 'AtHome',
      freshness: 'CheckIt',
      daysSinceBought: 4,
    },
  ],
};

const empty: IngredientBalance = { items: [] };

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;
  let shoppingApiMock: {
    getIngredientBalance: ReturnType<typeof vi.fn>;
    markAsFrozen: ReturnType<typeof vi.fn>;
  };

  function setupTestBed(getReturn: ReturnType<typeof vi.fn>): void {
    shoppingApiMock = {
      getIngredientBalance: getReturn,
      markAsFrozen: vi.fn(),
    };
    TestBed.configureTestingModule({
      imports: [PantryComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: ShoppingApiService, useValue: shoppingApiMock },
      ],
    });
    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
  }

  it('loads the balance on init and renders the items', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(shoppingApiMock.getIngredientBalance).toHaveBeenCalled();
    expect(component.items()).toHaveLength(3);
    const rendered = fixture.nativeElement.querySelectorAll('[data-testid="pantry-item"]');
    expect(rendered.length).toBe(3);
  }));

  it('renders a freshness chip only for tracked items', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const chips = fixture.nativeElement.querySelectorAll('[data-testid="freshness-chip"]');
    // 2 of 3 items are tracked (Chicken=UseSoon, Spinach=CheckIt); Salt is
    // NotTracked.
    expect(chips.length).toBe(2);
  }));

  it('shows freeze buttons only for at-home tracked items', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('[data-testid="pantry-freeze-btn"]');
    expect(buttons.length).toBe(2);
  }));

  it('calls markAsFrozen and re-loads on freeze', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    shoppingApiMock.markAsFrozen.mockReturnValue(of(undefined));
    shoppingApiMock.getIngredientBalance.mockClear();
    shoppingApiMock.getIngredientBalance.mockReturnValue(of(empty));

    const button = fixture.nativeElement.querySelector(
      '[data-testid="pantry-freeze-btn"]',
    ) as HTMLButtonElement;
    button.click();
    tick();

    expect(shoppingApiMock.markAsFrozen).toHaveBeenCalledWith({
      name: 'Chicken breast',
      unit: 'g',
    });
    expect(shoppingApiMock.getIngredientBalance).toHaveBeenCalled();
  }));

  it('shows the empty state when nothing is at home', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(empty)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const empty_ = fixture.nativeElement.querySelector('.empty-state');
    expect(empty_).toBeTruthy();
    expect(empty_.textContent).toContain('Nothing in your pantry yet');
  }));

  it('surfaces error on load failure', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(throwError(() => new Error('boom'))));
    fixture.detectChanges();
    tick();

    expect(component.serverError()).toBe('shopping.pantry.errors.loadFailed');
  }));
});
