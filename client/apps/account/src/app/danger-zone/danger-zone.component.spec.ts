import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '@yumney/shared/auth';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { DangerZoneComponent } from './danger-zone.component';
import { UserProfileApiService } from '../api';

const en = {
  account: {
    danger: {
      title: 'Danger Zone',
      lead: 'Lead',
      deleteAccount: 'Delete account',
      deleteIntro: 'Intro',
      consequence: {
        recipes: 'Recipes go',
        shoppingLists: 'Lists go',
        mealPlans: 'Plans go',
        profile: 'Profile goes',
        signIn: 'Sign-in goes',
      },
      confirmInstruction: 'Type {{token}} to confirm',
      confirmAria: 'Type the confirmation token',
      deleting: 'Deleting…',
      retry: 'Try again',
    },
  },
};

describe('DangerZoneComponent', () => {
  let fixture: ComponentFixture<DangerZoneComponent>;
  let apiMock: { deleteAccount: ReturnType<typeof vi.fn> };
  let authMock: { logout: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiMock = { deleteAccount: vi.fn().mockReturnValue(of(undefined)) };
    authMock = { logout: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [DangerZoneComponent, setupTranslocoTesting(en)],
      providers: [
        { provide: UserProfileApiService, useValue: apiMock },
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DangerZoneComponent);
    fixture.detectChanges();
  });

  function getButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('[data-testid="delete-account-btn"]');
  }

  function getInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input[type="text"]');
  }

  function typeConfirmation(value: string): void {
    fixture.componentInstance['confirmationInput'].set(value);
    fixture.detectChanges();
  }

  it('should disable the delete button until DELETE is typed exactly', () => {
    expect(getButton().disabled).toBe(true);

    typeConfirmation('delete');
    expect(getButton().disabled).toBe(true);

    typeConfirmation('DELETO');
    expect(getButton().disabled).toBe(true);

    typeConfirmation('DELETE');
    expect(getButton().disabled).toBe(false);
  });

  it('should call deleteAccount when the button is clicked with valid confirmation', () => {
    typeConfirmation('DELETE');

    getButton().click();

    expect(apiMock.deleteAccount).toHaveBeenCalledTimes(1);
  });

  it('should not call deleteAccount when the confirmation is wrong', () => {
    typeConfirmation('cancel');

    fixture.componentInstance['onDelete']();

    expect(apiMock.deleteAccount).not.toHaveBeenCalled();
  });

  it('should sign the user out after a successful deletion', () => {
    typeConfirmation('DELETE');

    getButton().click();

    expect(authMock.logout).toHaveBeenCalledTimes(1);
  });

  it('should not sign out when deletion fails', () => {
    apiMock.deleteAccount.mockReturnValue(throwError(() => new Error('boom')));
    typeConfirmation('DELETE');

    getButton().click();

    expect(authMock.logout).not.toHaveBeenCalled();
  });

  it('should accept text input via the confirmation field', () => {
    const input = getInput();
    expect(input).toBeTruthy();
    expect(input.getAttribute('autocomplete')).toBe('off');
  });
});
