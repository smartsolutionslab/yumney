import { Component, ChangeDetectionStrategy, ViewEncapsulation, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

export type CardVariant = 'auth';

@Component({
  selector: 'yn-card',
  imports: [TranslocoPipe],
  templateUrl: './card.component.html',
  styleUrl: './card.component.scss',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardComponent {
  variant = input<CardVariant>('auth');
  title = input<string | undefined>(undefined);
  subtitle = input<string | undefined>(undefined);

  protected readonly cardClass = computed(() => `${this.variant()}-card`);
}
