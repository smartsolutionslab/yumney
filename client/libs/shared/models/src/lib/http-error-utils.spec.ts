import { HttpErrorResponse } from '@angular/common/http';
import { mapHttpError, HttpErrorMap } from './http-error-utils';

describe('http-error-utils', () => {
  const errorMap: HttpErrorMap = {
    404: 'errors.notFound',
    409: 'errors.conflict',
    default: 'errors.generic',
  };

  describe('mapHttpError', () => {
    it('should return mapped error for known status', () => {
      const error = new HttpErrorResponse({ status: 404 });

      expect(mapHttpError(error, errorMap)).toBe('errors.notFound');
    });

    it('should return mapped error for another known status', () => {
      const error = new HttpErrorResponse({ status: 409 });

      expect(mapHttpError(error, errorMap)).toBe('errors.conflict');
    });

    it('should return default error for unknown status', () => {
      const error = new HttpErrorResponse({ status: 500 });

      expect(mapHttpError(error, errorMap)).toBe('errors.generic');
    });
  });
});
