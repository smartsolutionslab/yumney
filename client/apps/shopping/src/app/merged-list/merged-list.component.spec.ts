import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MergedListComponent } from './merged-list.component';
import { ShoppingApiService, type MergedShoppingList } from '../api';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';

const en = {
  shopping: {
    title: 'Shopping List',
    loading: 'Loading...',
    empty: 'Empty list',
    addPlaceholder: 'Add item...',
    export: 'Export',
    remove: 'Remove',
    checked: 'checked',
    retry: 'Retry',
  },
};

const mockList: MergedShoppingList = {
  items: [
    {
      itemName: 'Milk',
      totalQuantity: 2,
      displayQuantity: 2,
      unit: 'L',
      category: 'dairy',
      isBought: false,
      sources: [],
    },
    {
      itemName: 'Chicken',
      totalQuantity: 500,
      displayQuantity: 500,
      unit: 'g',
      category: 'meat-fish',
      isBought: false,
      sources: [],
    },
  ],
};

describe('MergedListComponent', () => {
  let fixture: ComponentFixture<MergedListComponent>;
  let apiMock: {
    getMergedList: ReturnType<typeof vi.fn>;
    addItem: ReturnType<typeof vi.fn>;
    removeItem: ReturnType<typeof vi.fn>;
    exportList: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    apiMock = {
      getMergedList: vi.fn().mockReturnValue(of(mockList)),
      addItem: vi.fn().mockReturnValue(of({ itemName: 'Eggs' })),
      removeItem: vi.fn().mockReturnValue(of(undefined)),
      exportList: vi.fn().mockReturnValue(of('Shopping list text')),
    };

    await TestBed.configureTestingModule({
      imports: [MergedListComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: ShoppingApiService, useValue: apiMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MergedListComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should load list on init', () => {
    expect(apiMock.getMergedList).toHaveBeenCalled();
  });

  it('should display items grouped by category', () => {
    const groups = fixture.nativeElement.querySelectorAll('.category-group');
    expect(groups.length).toBe(2);
  });

  it('should show item names', () => {
    expect(fixture.nativeElement.textContent).toContain('Milk');
    expect(fixture.nativeElement.textContent).toContain('Chicken');
  });

  it('should show add item input', () => {
    const input = fixture.nativeElement.querySelector('.add-input');
    expect(input).toBeTruthy();
  });

  it('should call addItem when submitting', () => {
    fixture.componentInstance['newItemName'].set('Eggs');
    fixture.componentInstance['onAddItem']();

    expect(apiMock.addItem).toHaveBeenCalledWith({ name: 'Eggs' });
  });

  it('should call removeItem', () => {
    fixture.componentInstance['onRemoveItem']('Milk');

    expect(apiMock.removeItem).toHaveBeenCalledWith({ name: 'Milk' });
  });

  it('should show error with retry on failure', () => {
    apiMock.getMergedList.mockReturnValue(throwError(() => new Error('fail')));
    fixture.componentInstance['loadList']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.retry-btn')).toBeTruthy();
  });

  it('should show empty state when no items', () => {
    apiMock.getMergedList.mockReturnValue(of({ items: [] }));
    fixture.componentInstance['loadList']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.empty-state')).toBeTruthy();
  });

  it('should show progress when items exist', () => {
    expect(fixture.nativeElement.querySelector('.progress-bar')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('0 / 2');
  });
});
