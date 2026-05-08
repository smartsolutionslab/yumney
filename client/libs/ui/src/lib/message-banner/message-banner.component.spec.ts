import { ComponentFixture, TestBed } from '@angular/core/testing';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { MessageBannerComponent } from './message-banner.component';

const en = {
  errors: {
    saveFailed: 'Failed to save the recipe',
    invalidUrl: 'The URL "{{ url }}" is not valid',
  },
  success: {
    saved: 'Saved {{ title }}',
  },
};

describe('MessageBannerComponent', () => {
  let fixture: ComponentFixture<MessageBannerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessageBannerComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(MessageBannerComponent);
  });

  // ── Tone-based classes ─────────────────────────────────────────────────────

  it('should render the error-banner class for the error tone', () => {
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('message', 'errors.saveFailed');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.classList.contains('error-banner')).toBe(true);
  });

  it('should render the success-banner class for the success tone', () => {
    fixture.componentRef.setInput('tone', 'success');
    fixture.componentRef.setInput('message', 'success.saved');
    fixture.componentRef.setInput('params', { title: 'Pasta' });

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.classList.contains('success-banner')).toBe(true);
  });

  // ── ARIA wiring ────────────────────────────────────────────────────────────

  it('should set role="alert" for the error tone', () => {
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('message', 'errors.saveFailed');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.getAttribute('role')).toBe('alert');
    expect(banner.getAttribute('aria-live')).toBeNull();
  });

  it('should set role="status" with aria-live="polite" for the success tone', () => {
    fixture.componentRef.setInput('tone', 'success');
    fixture.componentRef.setInput('message', 'success.saved');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.getAttribute('role')).toBe('status');
    expect(banner.getAttribute('aria-live')).toBe('polite');
  });

  // ── Translation rendering ──────────────────────────────────────────────────

  it('should translate the message key', () => {
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('message', 'errors.saveFailed');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.textContent.trim()).toBe('Failed to save the recipe');
  });

  it('should interpolate params when provided', () => {
    fixture.componentRef.setInput('tone', 'success');
    fixture.componentRef.setInput('message', 'success.saved');
    fixture.componentRef.setInput('params', { title: 'Pasta' });

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.textContent.trim()).toBe('Saved Pasta');
  });

  // ── Test id passthrough ────────────────────────────────────────────────────

  it('should set data-testid when testId input is provided', () => {
    fixture.componentRef.setInput('tone', 'success');
    fixture.componentRef.setInput('message', 'success.saved');
    fixture.componentRef.setInput('params', { title: 'Pasta' });
    fixture.componentRef.setInput('testId', 'success-banner');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.getAttribute('data-testid')).toBe('success-banner');
  });

  it('should omit data-testid when testId input is not provided', () => {
    fixture.componentRef.setInput('tone', 'error');
    fixture.componentRef.setInput('message', 'errors.saveFailed');

    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('div');
    expect(banner.getAttribute('data-testid')).toBeNull();
  });
});
