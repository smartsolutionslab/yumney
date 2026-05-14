import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { MergedListComponent } from './merged-list.component';
import { ShoppingApiService, type MergedShoppingList } from '../api';
import { setupTranslocoTesting, ToastService } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';

const en = {
  shopping: {
    title: 'Shopping List',
    loading: 'Loading...',
    empty: 'Empty list',
    addPlaceholder: 'Add item...',
    export: {
      button: 'Export list',
      copied: 'Copied to clipboard',
      shared: 'Shared',
      nothing: 'Nothing to export',
    },
    history: {
      toggle: 'Show past purchases',
      empty: 'No past purchases to show.',
    },
    sources: {
      toggle: 'Show sources',
      header: 'Sources',
      manual: 'Added manually',
      empty: 'No source breakdown available.',
    },
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
      addItem: vi.fn().mockReturnValue(
        of({
          itemName: 'Eggs',
          quantity: 1,
          unit: null,
          category: 'other',
          source: 'manual',
          ledgerIdentifier: '00000000-0000-0000-0000-000000000001',
        }),
      ),
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
    const groups = fixture.nativeElement.querySelectorAll(
      '[data-testid="shopping-category-group"]',
    );
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

  describe('past-purchases toggle', () => {
    it('requests active list (includePastBought=false) on load', () => {
      expect(apiMock.getMergedList).toHaveBeenCalledWith(false);
    });

    it('toggles and reloads with includePastBought=true', () => {
      apiMock.getMergedList.mockClear();
      apiMock.getMergedList.mockReturnValue(of(mockList));

      fixture.componentInstance['onTogglePastPurchases']();

      expect(fixture.componentInstance['showPastPurchases']()).toBe(true);
      expect(apiMock.getMergedList).toHaveBeenCalledWith(true);
    });

    it('toggling twice returns to the default view', () => {
      fixture.componentInstance['onTogglePastPurchases']();
      apiMock.getMergedList.mockClear();
      apiMock.getMergedList.mockReturnValue(of(mockList));

      fixture.componentInstance['onTogglePastPurchases']();

      expect(fixture.componentInstance['showPastPurchases']()).toBe(false);
      expect(apiMock.getMergedList).toHaveBeenCalledWith(false);
    });

    it('empty history shows the history-specific empty message', () => {
      apiMock.getMergedList.mockReturnValue(of({ items: [] }));
      fixture.componentInstance['showPastPurchases'].set(true);
      fixture.componentInstance['loadList']();
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('.empty-state');
      expect(empty?.textContent).toContain('No past purchases');
    });

    it('empty active list shows the default empty message', () => {
      apiMock.getMergedList.mockReturnValue(of({ items: [] }));
      fixture.componentInstance['loadList']();
      fixture.detectChanges();

      const empty = fixture.nativeElement.querySelector('.empty-state');
      expect(empty?.textContent).not.toContain('No past purchases');
    });

    it('stale in-flight response is discarded when a newer request starts', () => {
      const firstSubject = new Subject<MergedShoppingList>();
      const secondResponse: MergedShoppingList = { items: [] };

      apiMock.getMergedList.mockReturnValueOnce(firstSubject);
      fixture.componentInstance['onTogglePastPurchases']();

      apiMock.getMergedList.mockReturnValueOnce(of(secondResponse));
      fixture.componentInstance['onTogglePastPurchases']();

      // First (now stale) response arrives last; should be ignored.
      firstSubject.next(mockList);
      firstSubject.complete();

      expect(fixture.componentInstance['list']()).toEqual(secondResponse);
    });
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

  describe('export', () => {
    let toasts: ToastService;
    let shareSpy: ReturnType<typeof vi.fn>;
    let writeTextSpy: ReturnType<typeof vi.fn>;

    beforeEach(() => {
      toasts = TestBed.inject(ToastService);
      toasts.clear();

      shareSpy = vi.fn().mockResolvedValue(undefined);
      writeTextSpy = vi.fn().mockResolvedValue(undefined);

      vi.stubGlobal('navigator', {
        share: shareSpy,
        clipboard: { writeText: writeTextSpy },
      });
    });

    afterEach(() => {
      vi.unstubAllGlobals();
    });

    it('shows info toast when server returns no items', async () => {
      apiMock.exportList.mockReturnValue(of(''));

      fixture.componentInstance['onExport']();
      await Promise.resolve();

      expect(shareSpy).not.toHaveBeenCalled();
      expect(writeTextSpy).not.toHaveBeenCalled();
      expect(toasts.toasts()).toHaveLength(1);
      expect(toasts.toasts()[0]).toMatchObject({
        kind: 'info',
        messageKey: 'shopping.export.nothing',
      });
    });

    it('uses Web Share API when available and toasts shared on success', async () => {
      fixture.componentInstance['onExport']();
      await Promise.resolve();
      await Promise.resolve();

      expect(shareSpy).toHaveBeenCalledWith({ text: 'Shopping list text' });
      expect(toasts.toasts()[0]).toMatchObject({
        kind: 'success',
        messageKey: 'shopping.export.shared',
      });
    });

    it('falls back to clipboard when Web Share API is unavailable', async () => {
      vi.stubGlobal('navigator', { clipboard: { writeText: writeTextSpy } });

      fixture.componentInstance['onExport']();
      await Promise.resolve();
      await Promise.resolve();

      expect(writeTextSpy).toHaveBeenCalledWith('Shopping list text');
      expect(toasts.toasts()[0]).toMatchObject({
        kind: 'success',
        messageKey: 'shopping.export.copied',
      });
    });

    it('does not fall back to clipboard when user aborts the share sheet', async () => {
      const abort = new DOMException('Share canceled', 'AbortError');
      shareSpy.mockRejectedValue(abort);

      fixture.componentInstance['onExport']();
      await Promise.resolve();
      await Promise.resolve();

      expect(writeTextSpy).not.toHaveBeenCalled();
      expect(toasts.toasts()).toHaveLength(0);
    });

    it('falls back to clipboard when share fails for a non-abort reason', async () => {
      shareSpy.mockRejectedValue(new Error('denied'));

      fixture.componentInstance['onExport']();
      await Promise.resolve();
      await Promise.resolve();
      await Promise.resolve();

      expect(writeTextSpy).toHaveBeenCalledWith('Shopping list text');
      expect(toasts.toasts()[0]).toMatchObject({
        kind: 'success',
        messageKey: 'shopping.export.copied',
      });
    });
  });

  describe('expandable sources (US-313)', () => {
    const listWithSources: MergedShoppingList = {
      items: [
        {
          itemName: 'Milk',
          totalQuantity: 3,
          displayQuantity: 3,
          unit: 'L',
          category: 'dairy',
          isBought: false,
          sources: [
            { quantity: 2, source: 'Lasagne', occurredAt: '2026-05-09T10:00:00Z' },
            { quantity: 1, source: 'manual', occurredAt: '2026-05-10T08:30:00Z' },
          ],
        },
        {
          itemName: 'Flour',
          totalQuantity: 500,
          displayQuantity: 500,
          unit: 'g',
          category: 'pantry',
          isBought: false,
          sources: [],
        },
      ],
    };

    beforeEach(async () => {
      apiMock.getMergedList.mockReturnValue(of(listWithSources));
      fixture.componentInstance['loadList']();
      fixture.detectChanges();
    });

    it('renders the merged total per item', () => {
      expect(fixture.nativeElement.textContent).toContain('3 L');
      expect(fixture.nativeElement.textContent).toContain('Milk');
    });

    it('starts collapsed — no source-breakdown panel visible', () => {
      const panels = fixture.nativeElement.querySelectorAll(
        '[data-testid="shopping-sources-panel"]',
      );
      expect(panels.length).toBe(0);
    });

    it('expands the breakdown when an item row is clicked', () => {
      const rows = fixture.nativeElement.querySelectorAll('[data-testid="shopping-item-row"]');
      rows[0].click();
      fixture.detectChanges();

      const panel = fixture.nativeElement.querySelector('[data-testid="shopping-sources-panel"]');
      expect(panel).toBeTruthy();
      expect(rows[0].getAttribute('aria-expanded')).toBe('true');
    });

    it('collapses the breakdown when the same row is clicked again', () => {
      const rows = fixture.nativeElement.querySelectorAll('[data-testid="shopping-item-row"]');
      rows[0].click();
      fixture.detectChanges();
      rows[0].click();
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelectorAll('[data-testid="shopping-sources-panel"]').length,
      ).toBe(0);
    });

    it('renders one source row per ItemSource entry', () => {
      const rows = fixture.nativeElement.querySelectorAll('[data-testid="shopping-item-row"]');
      rows[0].click();
      fixture.detectChanges();

      const sourceRows = fixture.nativeElement.querySelectorAll(
        '[data-testid="shopping-source-row"]',
      );
      expect(sourceRows.length).toBe(2);
      const text = fixture.nativeElement.textContent;
      expect(text).toContain('Lasagne');
      expect(text).toContain('Added manually');
    });

    it('shows empty-source message when an item has no sources', () => {
      const rows = fixture.nativeElement.querySelectorAll('[data-testid="shopping-item-row"]');
      rows[1].click();
      fixture.detectChanges();

      const panel = fixture.nativeElement.querySelector('[data-testid="shopping-sources-panel"]');
      expect(panel.textContent).toContain('No source breakdown available');
    });

    it('expands items independently', () => {
      const rows = fixture.nativeElement.querySelectorAll('[data-testid="shopping-item-row"]');
      rows[0].click();
      fixture.detectChanges();

      expect(rows[0].getAttribute('aria-expanded')).toBe('true');
      expect(rows[1].getAttribute('aria-expanded')).toBe('false');
    });

    it('clicking the remove button does not toggle expansion', () => {
      const containers = fixture.nativeElement.querySelectorAll('.item-row-container');
      const row = containers[0].querySelector('[data-testid="shopping-item-row"]');
      const removeButton = containers[0].querySelector('.remove-btn');
      removeButton.click();
      fixture.detectChanges();

      // Row stays collapsed; only the remove API call should fire.
      expect(row.getAttribute('aria-expanded')).toBe('false');
      expect(apiMock.removeItem).toHaveBeenCalledWith({ name: 'Milk' });
    });
  });
});
