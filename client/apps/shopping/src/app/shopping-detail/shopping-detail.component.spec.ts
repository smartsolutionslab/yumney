import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ShoppingDetailComponent } from './shopping-detail.component';
import { ShoppingApiService, ShoppingListDetail } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';

const mockDetail: ShoppingListDetail = {
  identifier: 'list-123',
  title: 'Weekly Groceries',
  recipeIdentifier: 'recipe-abc',
  createdAt: '2026-03-10T00:00:00Z',
  items: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
  ],
};

const en = {
  shopping: {
    detail: {
      back: 'Back',
      loading: 'Loading...',
      viewRecipe: 'View Recipe',
      errors: {
        notFound: 'Shopping list not found.',
        generic: 'Failed to load shopping list.',
      },
    },
  },
};

describe('ShoppingDetailComponent', () => {
  let component: ShoppingDetailComponent;
  let fixture: ComponentFixture<ShoppingDetailComponent>;
  let shoppingApiMock: { getShoppingListById: ReturnType<typeof vi.fn> };

  function setupTestBed(
    apiReturn = vi.fn().mockReturnValue(of(mockDetail)),
    identifier: string | null = 'list-123',
  ) {
    shoppingApiMock = { getShoppingListById: apiReturn };

    TestBed.configureTestingModule({
      imports: [
        ShoppingDetailComponent,
        setupTranslocoTesting(en),
      ],
      providers: [
        provideRouter([]),
        { provide: ShoppingApiService, useValue: shoppingApiMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'identifier' ? identifier : null),
              },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(ShoppingDetailComponent);
    component = fixture.componentInstance;
  }

  it('should load shopping list on init', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    expect(shoppingApiMock.getShoppingListById).toHaveBeenCalledWith('list-123');
    expect(component.shoppingList()).toEqual(mockDetail);
    expect(component.isLoading()).toBe(false);
  }));

  it('should render list items', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.items-list li');
    expect(items.length).toBe(2);
  }));

  it('should show error when identifier is missing', fakeAsync(() => {
    setupTestBed(vi.fn(), null);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(component.serverError()).toBe('shopping.detail.errors.notFound');
    expect(shoppingApiMock.getShoppingListById).not.toHaveBeenCalled();
  }));

  it('should show error on 404 response', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 404 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Shopping list not found.');
  }));

  it('should show generic error on 500 response', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to load shopping list.');
  }));

  it('should show loading state initially', fakeAsync(() => {
    const subject = new Subject<ShoppingListDetail>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);

    subject.next(mockDetail);
    subject.complete();
    tick();

    expect(component.isLoading()).toBe(false);
  }));
});
