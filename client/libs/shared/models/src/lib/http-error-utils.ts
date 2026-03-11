import { HttpErrorResponse } from '@angular/common/http';

export type HttpErrorMap = Record<number, string> & { default: string };

export function mapHttpError(error: HttpErrorResponse, errorMap: HttpErrorMap): string {
  return errorMap[error.status] ?? errorMap.default;
}
