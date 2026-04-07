import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { FormFieldComponent } from './form-field.component';

const en = {
  form: {
    nameLabel: 'Name',
    required: 'Field is required',
    minLength: 'Field is too short',
    passwordsMismatch: 'Passwords do not match',
  },
};

describe('FormFieldComponent', () => {
  let fixture: ComponentFixture<FormFieldComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormFieldComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(FormFieldComponent);
  });

  // ── Label rendering ────────────────────────────────────────────────────────

  it('should render the translated label when label input is set', () => {
    const control = new FormControl('');
    fixture.componentRef.setInput('label', 'form.nameLabel');
    fixture.componentRef.setInput('control', control);

    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('label');
    expect(label.textContent).toContain('Name');
  });

  it('should not render a label element when label input is empty', () => {
    const control = new FormControl('');
    fixture.componentRef.setInput('control', control);

    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('label');
    expect(label).toBeFalsy();
  });

  // ── Control error display ──────────────────────────────────────────────────

  it('should not display errors when control is untouched', () => {
    const control = new FormControl('', Validators.required);
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errors', { required: 'form.required' });

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error).toBeFalsy();
  });

  it('should display the error when control is touched and invalid', () => {
    const control = new FormControl('', Validators.required);
    control.markAsTouched();
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errors', { required: 'form.required' });

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Field is required');
  });

  it('should not display errors when control is touched but valid', () => {
    const control = new FormControl('something', Validators.required);
    control.markAsTouched();
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errors', { required: 'form.required' });

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error).toBeFalsy();
  });

  it('should display the matching error when multiple error keys are configured', () => {
    const control = new FormControl('a', [Validators.required, Validators.minLength(5)]);
    control.markAsTouched();
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('errors', {
      required: 'form.required',
      minlength: 'form.minLength',
    });

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Field is too short');
  });

  // ── Group-level error display ──────────────────────────────────────────────

  it('should display group errors only when the control is touched', () => {
    const control = new FormControl('');
    const group = new FormGroup({ password: control }, { validators: () => ({ passwordsMismatch: true }) });
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('group', group);
    fixture.componentRef.setInput('groupErrors', { passwordsMismatch: 'form.passwordsMismatch' });
    control.markAsTouched();

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error.textContent).toContain('Passwords do not match');
  });

  it('should not display group errors when the control is untouched', () => {
    const control = new FormControl('');
    const group = new FormGroup({ password: control }, { validators: () => ({ passwordsMismatch: true }) });
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('group', group);
    fixture.componentRef.setInput('groupErrors', { passwordsMismatch: 'form.passwordsMismatch' });

    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error).toBeFalsy();
  });
});
