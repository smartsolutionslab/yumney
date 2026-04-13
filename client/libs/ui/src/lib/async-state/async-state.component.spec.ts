import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { AsyncStateComponent } from './async-state.component';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = { common: { loading: 'Loading...', retry: 'Retry' } };

@Component({
  standalone: true,
  imports: [AsyncStateComponent],
  template: `
    <yn-async-state
      [loading]="loading()"
      [error]="error()"
      [loadingKey]="'common.loading'"
      [retryKey]="'common.retry'"
      (retry)="onRetry()"
    />
  `,
})
class TestHostComponent {
  loading = signal(false);
  error = signal<string | null>(null);
  retried = false;
  onRetry(): void {
    this.retried = true;
  }
}

describe('AsyncStateComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent, setupTranslocoTesting(en)],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should not show anything when idle', () => {
    expect(fixture.nativeElement.querySelector('.loading')).toBeFalsy();
    expect(fixture.nativeElement.querySelector('.error')).toBeFalsy();
  });

  it('should show loading state', () => {
    fixture.componentInstance.loading.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.loading')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Loading...');
  });

  it('should show error with retry button', () => {
    fixture.componentInstance.error.set('Something failed');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Something failed');
    expect(fixture.nativeElement.querySelector('.retry-btn')).toBeTruthy();
  });

  it('should emit retry on button click', () => {
    fixture.componentInstance.error.set('Error');
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.retry-btn').click();

    expect(fixture.componentInstance.retried).toBe(true);
  });
});
