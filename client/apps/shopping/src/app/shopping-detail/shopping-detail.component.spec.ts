import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ShoppingDetailComponent } from './shopping-detail.component';
import { ShoppingApiService, ShoppingListDetail } from '../api';
import { setupTranslocoTesting, ToastService } from '@yumney/shared/models';

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
      checkAll: 'Check all',
      reset: 'Reset',
      errors: {
        notFound: 'Shopping list not found.',
        generic: 'Failed to load shopping list.',
      },
      bring: {
        sendButton: 'Send to Bring!',
        sent: 'Sent {{count}} items to Bring!',
        notInstalled: 'Get Bring! at {{fallbackUrl}}',
      },
    },
  },
};

describe('ShoppingDetailComponent', () => {
  let component: ShoppingDetailComponent;
  let fixture: ComponentFixture<ShoppingDetailComponent>;
  let shoppingApiMock: { getShoppingListById: ReturnType<typeof vi.fn> };

  function setupTestBed(apiReturn = vi.fn().mockReturnValue(of(mockDetail)), identifier: string | null = 'list-123') {
    shoppingApiMock = { getShoppingListById: apiReturn };

    TestBed.configureTestingModule({
      imports: [ShoppingDetailComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
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

  describe('Send to Bring!', () => {
    let navigateSpy: ReturnType<typeof vi.fn>;

    function stubNavigate() {
      navigateSpy = vi.fn();
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (component as any).navigate = navigateSpy;
    }

    it('should disable the button until at least one item is unchecked', fakeAsync(() => {
      const allChecked: ShoppingListDetail = {
        ...mockDetail,
        items: mockDetail.items.map((item) => ({ ...item, isChecked: true })),
      };
      setupTestBed(vi.fn().mockReturnValue(of(allChecked)));
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const btn: HTMLButtonElement = fixture.nativeElement.querySelector('[data-testid="send-to-bring-btn"]');
      expect(btn.disabled).toBe(true);
    }));

    it('should navigate to a bring:// URL with unchecked items only', fakeAsync(() => {
      setupTestBed();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
      stubNavigate();

      fixture.nativeElement.querySelector('[data-testid="send-to-bring-btn"]').click();

      expect(navigateSpy).toHaveBeenCalledTimes(1);
      const url = navigateSpy.mock.calls[0][0] as string;
      expect(url).toMatch(/^bring:\/\/import\?items=/);
      expect(decodeURIComponent(url)).toContain('Spaghetti');
      expect(decodeURIComponent(url)).toContain('Eggs');

      // Drain the visibility-probe timeout so the fakeAsync zone settles.
      tick(1500);
    }));

    it('should show a "sent" toast immediately', fakeAsync(() => {
      setupTestBed();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
      stubNavigate();

      const toast = TestBed.inject(ToastService);
      const successSpy = vi.spyOn(toast, 'success');

      fixture.nativeElement.querySelector('[data-testid="send-to-bring-btn"]').click();

      expect(successSpy).toHaveBeenCalledWith('shopping.detail.bring.sent', { count: 2 });
      tick(1500);
    }));

    it('should show fallback toast if the document is still visible after the probe window', fakeAsync(() => {
      setupTestBed();
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
      stubNavigate();

      const toast = TestBed.inject(ToastService);
      const warningSpy = vi.spyOn(toast, 'warning');

      // jsdom returns 'visible' by default; explicit assert + then trigger.
      expect(document.visibilityState).toBe('visible');

      fixture.nativeElement.querySelector('[data-testid="send-to-bring-btn"]').click();

      tick(1500);
      expect(warningSpy).toHaveBeenCalled();
      expect(warningSpy.mock.calls[0][0]).toBe('shopping.detail.bring.notInstalled');
    }));
  });
});
