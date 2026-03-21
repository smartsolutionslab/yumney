import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { AccountPlaceholderComponent } from './account-placeholder.component';

const en = {
  account: {
    placeholder: {
      title: 'My Account',
      message: 'Account management coming soon.',
    },
  },
};

describe('AccountPlaceholderComponent', () => {
  let fixture: ComponentFixture<AccountPlaceholderComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [
        AccountPlaceholderComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
        }),
      ],
    });

    fixture = TestBed.createComponent(AccountPlaceholderComponent);
    fixture.detectChanges();
  });

  it('should render the placeholder title', () => {
    const title = fixture.nativeElement.querySelector('h1');
    expect(title).toBeTruthy();
    expect(title.textContent).toContain('My Account');
  });

  it('should render the placeholder message', () => {
    const message = fixture.nativeElement.querySelector('p');
    expect(message).toBeTruthy();
    expect(message.textContent).toContain('Account management coming soon.');
  });
});
