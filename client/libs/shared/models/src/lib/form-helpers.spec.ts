import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ensureFormValid } from './form-helpers';

describe('ensureFormValid', () => {
  it('returns true when the form is valid', () => {
    const form = new FormGroup({
      email: new FormControl('hi@example.com', Validators.required),
    });

    expect(ensureFormValid(form)).toBe(true);
  });

  it('returns false and marks every control as touched when invalid', () => {
    const form = new FormGroup({
      email: new FormControl('', Validators.required),
      name: new FormControl('', Validators.required),
    });

    const result = ensureFormValid(form);

    expect(result).toBe(false);
    expect(form.get('email')!.touched).toBe(true);
    expect(form.get('name')!.touched).toBe(true);
  });

  it('does not alter touched state on a valid form', () => {
    const form = new FormGroup({
      email: new FormControl('ok@example.com', Validators.required),
    });
    // starts pristine/untouched
    expect(form.get('email')!.touched).toBe(false);

    ensureFormValid(form);

    expect(form.get('email')!.touched).toBe(false);
  });
});
