import { FormGroup, FormArray } from '@angular/forms';

export function hasControlError(form: FormGroup, field: string, error: string): boolean {
  const control = form.get(field);
  return !!control?.hasError(error) && !!control?.touched;
}

export function hasArrayItemError(
  array: FormArray,
  index: number,
  field: string,
  error: string,
): boolean {
  const control = array.at(index)?.get(field);
  return !!control?.hasError(error) && !!control?.touched;
}
