import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'yn-loading-spinner',
  imports: [TranslocoModule],
  template: `
    <div class="loading">
      <span class="spinner"></span>
      <span>{{ label() | transloco }}</span>
    </div>
  `,
  styleUrl: './loading-spinner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingSpinnerComponent {
  label = input.required<string>();
}
