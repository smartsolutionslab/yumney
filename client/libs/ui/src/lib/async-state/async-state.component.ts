import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'yn-async-state',
  standalone: true,
  imports: [TranslocoModule],
  template: `
    @if (loading()) {
      <div class="loading" role="status" aria-live="polite">{{ loadingKey() | transloco }}</div>
    }
    @if (error(); as err) {
      <div class="error" role="alert">
        {{ err | transloco }}
        <button class="retry-btn" (click)="retry.emit()">
          {{ retryKey() | transloco }}
        </button>
      </div>
    }
  `,
  styleUrl: './async-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AsyncStateComponent {
  loading = input(false);
  error = input<string | null>(null);
  loadingKey = input('common.loading');
  retryKey = input('common.retry');
  retry = output<void>();
}
