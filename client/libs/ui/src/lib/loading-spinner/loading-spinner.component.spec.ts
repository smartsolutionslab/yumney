import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LoadingSpinnerComponent } from './loading-spinner.component';

const en = {
  loading: {
    recipes: 'Loading recipes',
    saving: 'Saving',
  },
};

describe('LoadingSpinnerComponent', () => {
  let fixture: ComponentFixture<LoadingSpinnerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LoadingSpinnerComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoadingSpinnerComponent);
  });

  it('should render the spinner element', () => {
    fixture.componentRef.setInput('label', 'loading.recipes');

    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.spinner');
    expect(spinner).toBeTruthy();
  });

  it('should translate the label input', () => {
    fixture.componentRef.setInput('label', 'loading.recipes');

    fixture.detectChanges();

    const text = fixture.nativeElement.querySelector('.loading')?.textContent;
    expect(text).toContain('Loading recipes');
  });

  it('should react to label input changes', () => {
    fixture.componentRef.setInput('label', 'loading.recipes');
    fixture.detectChanges();

    fixture.componentRef.setInput('label', 'loading.saving');
    fixture.detectChanges();

    const text = fixture.nativeElement.querySelector('.loading')?.textContent;
    expect(text).toContain('Saving');
  });
});
