import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ShoppingListComponent } from './shopping-list.component';
import { ShoppingApiService, ShoppingListSummary } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';

const mockLists: ShoppingListSummary[] = [
  {
    identifier: 'list-1',
    title: 'Weekly Groceries',
    itemCount: 5,
    createdAt: '2026-03-10T00:00:00Z',
  },
  {
    identifier: 'list-2',
    title: 'Party Supplies',
    itemCount: 3,
    createdAt: '2026-03-11T00:00:00Z',
  },
];

const en = {
  shopping: {
    list: {
      title: 'Shopping Lists',
      loading: 'Loading...',
      itemCount: '{{ count }} items',
      empty: {
        title: 'No shopping lists',
        message: 'Create your first shopping list from a recipe.',
      },
      errors: {
        generic: 'Failed to load shopping lists.',
      },
    },
  },
};

describe('ShoppingListComponent', () => {
  let component: ShoppingListComponent;
  let fixture: ComponentFixture<ShoppingListComponent>;
  let shoppingApiMock: { getShoppingLists: ReturnType<typeof vi.fn> };

  function setupTestBed(apiReturn = vi.fn().mockReturnValue(of(mockLists))) {
    shoppingApiMock = { getShoppingLists: apiReturn };

    TestBed.configureTestingModule({
      imports: [ShoppingListComponent, setupTranslocoTesting(en)],
      providers: [provideRouter([]), { provide: ShoppingApiService, useValue: shoppingApiMock }],
    });

    fixture = TestBed.createComponent(ShoppingListComponent);
    component = fixture.componentInstance;
  }

  it('should load shopping lists on init', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    expect(shoppingApiMock.getShoppingLists).toHaveBeenCalled();
    expect(component.lists()).toEqual(mockLists);
    expect(component.isLoading()).toBe(false);
  }));

  it('should render list cards', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.list-card');
    expect(cards.length).toBe(2);
  }));

  it('should show empty state when no lists', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of([])));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-state');
    expect(empty).toBeTruthy();
    expect(empty.textContent).toContain('No shopping lists');
  }));

  it('should show error on API failure', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to load shopping lists.');
  }));

  it('should show loading state initially', fakeAsync(() => {
    const subject = new Subject<ShoppingListSummary[]>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);

    subject.next(mockLists);
    subject.complete();
    tick();

    expect(component.isLoading()).toBe(false);
  }));
});
