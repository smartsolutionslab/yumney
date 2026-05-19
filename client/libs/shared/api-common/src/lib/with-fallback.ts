import { Observable, catchError, of } from 'rxjs';

/**
 * Returns an RxJS pipeable that swallows errors and emits the given fallback
 * value instead. Use only for non-critical reads where the UI must keep
 * working when the backend is unreachable (e.g., dashboard widgets).
 */
export function withFallback<T>(fallback: T): (source: Observable<T>) => Observable<T> {
  return (source) => source.pipe(catchError(() => of(fallback)));
}
