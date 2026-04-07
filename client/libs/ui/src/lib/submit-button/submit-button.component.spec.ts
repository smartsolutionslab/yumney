import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { SubmitButtonComponent } from './submit-button.component';

const en = {
  form: {
    save: 'Save',
    saving: 'Saving...',
  },
};

describe('SubmitButtonComponent', () => {
  let fixture: ComponentFixture<SubmitButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        SubmitButtonComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SubmitButtonComponent);
    fixture.componentRef.setInput('label', 'form.save');
    fixture.componentRef.setInput('loadingLabel', 'form.saving');
  });

  // ── Idle state ─────────────────────────────────────────────────────────────

  it('should render the label when not loading', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Save');
  });

  it('should be enabled when not loading and not disabled', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(false);
  });

  it('should default to type="submit"', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('type')).toBe('submit');
  });

  // ── Loading state ──────────────────────────────────────────────────────────

  it('should render the loading label when loading', () => {
    fixture.componentRef.setInput('loading', true);

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.textContent).toContain('Saving...');
  });

  it('should disable the button when loading', () => {
    fixture.componentRef.setInput('loading', true);

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  it('should render a spinner when loading and showSpinner is true', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.componentRef.setInput('showSpinner', true);

    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.btn-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should not render a spinner when loading but showSpinner is false', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.componentRef.setInput('showSpinner', false);

    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('.btn-spinner');
    expect(spinner).toBeFalsy();
  });

  // ── Disabled state ─────────────────────────────────────────────────────────

  it('should disable the button when disabled input is true', () => {
    fixture.componentRef.setInput('disabled', true);

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });

  // ── Custom attributes ─────────────────────────────────────────────────────

  it('should apply the cssClass input', () => {
    fixture.componentRef.setInput('cssClass', 'btn-primary');

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.classList.contains('btn-primary')).toBe(true);
  });

  it('should respect the type input override', () => {
    fixture.componentRef.setInput('type', 'button');

    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('type')).toBe('button');
  });
});
