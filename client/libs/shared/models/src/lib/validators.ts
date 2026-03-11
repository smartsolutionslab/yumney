import { AbstractControl, ValidationErrors } from '@angular/forms';

export function urlValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);
    if (url.protocol === 'http:' || url.protocol === 'https:') {
      return null;
    }
  } catch {
    // invalid URL
  }

  return { invalidUrl: true };
}

export function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  if (password && confirmPassword && password !== confirmPassword) {
    return { passwordsMismatch: true };
  }

  return null;
}
