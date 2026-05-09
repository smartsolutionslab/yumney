import {
  Component,
  ChangeDetectionStrategy,
  ViewEncapsulation,
  computed,
  input,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type EmptyStateVariant = 'card' | 'minimal';

@Component({
  selector: 'yn-empty-state',
  imports: [TranslocoPipe],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  variant = input<EmptyStateVariant>('card');
  title = input<string | undefined>(undefined);
  message = input.required<string>();
  messageParams = input<Record<string, unknown> | undefined>(undefined);
  testId = input<string | undefined>(undefined);

  protected readonly containerClass = computed(() => `empty-state empty-state--${this.variant()}`);
}
