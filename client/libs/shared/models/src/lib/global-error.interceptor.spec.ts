import { HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { globalErrorInterceptor } from './global-error.interceptor';
import { ToastService } from './toast.service';

describe('globalErrorInterceptor', () => {
  let toastError: ReturnType<typeof vi.fn>;

  const runThrough = <T>(next: () => ReturnType<typeof throwError | typeof of>) => {
    return TestBed.runInInjectionContext(() => {
      const req = new HttpRequest('GET', '/api/whatever');
      return firstValueFrom(globalErrorInterceptor(req, next as never));
    });
  };

  beforeEach(() => {
    toastError = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: ToastService, useValue: { error: toastError } }],
    });
  });

  it('passes through successful responses without firing a toast', async () => {
    const response = { body: 'ok' };
    const result = await runThrough(() => of(response) as never);

    expect(result).toBe(response);
    expect(toastError).not.toHaveBeenCalled();
  });

  it('shows the networkUnavailable toast for status 0 errors', async () => {
    const err = new HttpErrorResponse({ status: 0 });
    await expect(runThrough(() => throwError(() => err) as never)).rejects.toBe(err);

    expect(toastError).toHaveBeenCalledWith('common.errors.networkUnavailable');
  });

  it('shows the serviceUnavailable toast for status 503 errors', async () => {
    const err = new HttpErrorResponse({ status: 503 });
    await expect(runThrough(() => throwError(() => err) as never)).rejects.toBe(err);

    expect(toastError).toHaveBeenCalledWith('common.errors.serviceUnavailable');
  });

  it('does not toast for status codes it does not handle (e.g. 404)', async () => {
    const err = new HttpErrorResponse({ status: 404 });
    await expect(runThrough(() => throwError(() => err) as never)).rejects.toBe(err);

    expect(toastError).not.toHaveBeenCalled();
  });

  it('does not toast for non-HttpErrorResponse errors', async () => {
    const err = new Error('random');
    await expect(runThrough(() => throwError(() => err) as never)).rejects.toBe(err);

    expect(toastError).not.toHaveBeenCalled();
  });
});
