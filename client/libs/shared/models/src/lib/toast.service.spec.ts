import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({ providers: [ToastService] });
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should add a toast with kind info by default', () => {
    service.show({ messageKey: 'hello' });

    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].kind).toBe('info');
  });

  it('should assign unique incrementing ids', () => {
    const a = service.show({ messageKey: 'a' });
    const b = service.show({ messageKey: 'b' });

    expect(b).toBe(a + 1);
  });

  it('should use success kind when calling success()', () => {
    service.success('saved');

    expect(service.toasts()[0].kind).toBe('success');
  });

  it('should use error kind when calling error()', () => {
    service.error('boom');

    expect(service.toasts()[0].kind).toBe('error');
  });

  it('should use warning kind when calling warning()', () => {
    service.warning('careful');

    expect(service.toasts()[0].kind).toBe('warning');
  });

  it('should use info kind when calling info()', () => {
    service.info('hello');

    expect(service.toasts()[0].kind).toBe('info');
  });

  it('should forward params onto the toast', () => {
    service.show({ messageKey: 'greet', params: { name: 'Ada' } });

    expect(service.toasts()[0].params).toEqual({ name: 'Ada' });
  });

  it('should auto-dismiss after the configured duration', () => {
    service.show({ messageKey: 'hi', durationMs: 1000 });
    expect(service.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(1000);

    expect(service.toasts()).toHaveLength(0);
  });

  it('should not auto-dismiss when durationMs is zero', () => {
    service.show({ messageKey: 'sticky', durationMs: 0 });
    vi.advanceTimersByTime(60_000);

    expect(service.toasts()).toHaveLength(1);
  });

  it('should remove only the toast with the given id on dismiss', () => {
    const a = service.show({ messageKey: 'a', durationMs: 0 });
    service.show({ messageKey: 'b', durationMs: 0 });

    service.dismiss(a);

    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].messageKey).toBe('b');
  });

  it('should remove all toasts on clear', () => {
    service.show({ messageKey: 'a', durationMs: 0 });
    service.show({ messageKey: 'b', durationMs: 0 });

    service.clear();

    expect(service.toasts()).toEqual([]);
  });
});
