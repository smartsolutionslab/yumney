import { FormBuilder, FormArray, FormGroup, Validators } from '@angular/forms';
import { hasControlError, hasArrayItemError } from './form-utils';

describe('form-utils', () => {
  const fb = new FormBuilder();

  describe('hasControlError', () => {
    it('should return true when field has error and is touched', () => {
      const form = fb.group({ name: ['', Validators.required] });
      form.get('name')!.markAsTouched();

      expect(hasControlError(form, 'name', 'required')).toBe(true);
    });

    it('should return false when field has error but is untouched', () => {
      const form = fb.group({ name: ['', Validators.required] });

      expect(hasControlError(form, 'name', 'required')).toBe(false);
    });

    it('should return false when field has no error', () => {
      const form = fb.group({ name: ['valid', Validators.required] });
      form.get('name')!.markAsTouched();

      expect(hasControlError(form, 'name', 'required')).toBe(false);
    });

    it('should return false for non-existent field', () => {
      const form = fb.group({ name: [''] });

      expect(hasControlError(form, 'missing', 'required')).toBe(false);
    });
  });

  describe('hasArrayItemError', () => {
    let array: FormArray<FormGroup>;

    beforeEach(() => {
      array = fb.array([
        fb.group({ name: ['', Validators.required] }),
        fb.group({ name: ['valid', Validators.required] }),
      ]) as FormArray<FormGroup>;
    });

    it('should return true when array item field has error and is touched', () => {
      array.at(0).get('name')!.markAsTouched();

      expect(hasArrayItemError(array, 0, 'name', 'required')).toBe(true);
    });

    it('should return false when array item field has error but is untouched', () => {
      expect(hasArrayItemError(array, 0, 'name', 'required')).toBe(false);
    });

    it('should return false when array item field has no error', () => {
      array.at(1).get('name')!.markAsTouched();

      expect(hasArrayItemError(array, 1, 'name', 'required')).toBe(false);
    });
  });
});
