import { FormGroup } from '@angular/forms';

/**
 * Returns true if the form is valid. Otherwise marks all fields as touched
 * (so validation errors render) and returns false. Standard guard for
 * form submission handlers.
 */
export function ensureFormValid(form: FormGroup): boolean {
  if (form.invalid) {
    form.markAllAsTouched();
    return false;
  }
  return true;
}
