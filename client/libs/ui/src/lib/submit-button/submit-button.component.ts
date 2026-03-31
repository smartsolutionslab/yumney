import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'yn-submit-button',
  imports: [TranslocoPipe],
  templateUrl: './submit-button.component.html',
  styleUrl: './submit-button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubmitButtonComponent {
  label = input.required<string>();
  loadingLabel = input.required<string>();
  loading = input(false);
  disabled = input(false);
  showSpinner = input(false);
  type = input<'submit' | 'button'>('submit');
  cssClass = input('');
}
