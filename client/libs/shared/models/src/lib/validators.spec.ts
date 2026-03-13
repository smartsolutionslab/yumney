import { FormBuilder, FormControl } from '@angular/forms';
import { urlValidator, passwordsMatchValidator } from './validators';

describe('validators', () => {
  describe('urlValidator', () => {
    it('should return null for valid http URL', () => {
      const control = new FormControl('http://example.com');

      expect(urlValidator(control)).toBeNull();
    });

    it('should return null for valid https URL', () => {
      const control = new FormControl('https://example.com/path?q=1');

      expect(urlValidator(control)).toBeNull();
    });

    it('should return null for empty value', () => {
      const control = new FormControl('');

      expect(urlValidator(control)).toBeNull();
    });

    it('should return null for null value', () => {
      const control = new FormControl(null);

      expect(urlValidator(control)).toBeNull();
    });

    it('should return error for ftp URL', () => {
      const control = new FormControl('ftp://example.com');

      expect(urlValidator(control)).toEqual({ invalidUrl: true });
    });

    it('should return error for invalid URL', () => {
      const control = new FormControl('not-a-url');

      expect(urlValidator(control)).toEqual({ invalidUrl: true });
    });
  });

  describe('passwordsMatchValidator', () => {
    const fb = new FormBuilder();

    it('should return null when passwords match', () => {
      const group = fb.group({
        password: 'Secret123',
        confirmPassword: 'Secret123',
      });

      expect(passwordsMatchValidator(group)).toBeNull();
    });

    it('should return error when passwords do not match', () => {
      const group = fb.group({
        password: 'Secret123',
        confirmPassword: 'Different',
      });

      expect(passwordsMatchValidator(group)).toEqual({ passwordsMismatch: true });
    });

    it('should return null when password is empty', () => {
      const group = fb.group({
        password: '',
        confirmPassword: 'Secret123',
      });

      expect(passwordsMatchValidator(group)).toBeNull();
    });

    it('should return null when confirmPassword is empty', () => {
      const group = fb.group({
        password: 'Secret123',
        confirmPassword: '',
      });

      expect(passwordsMatchValidator(group)).toBeNull();
    });
  });
});
