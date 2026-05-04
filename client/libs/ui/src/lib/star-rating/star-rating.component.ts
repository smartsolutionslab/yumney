import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  output,
  signal,
  viewChildren,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { prefersReducedMotion, springPress } from '../animation/gsap-utils';

const STARS = [1, 2, 3, 4, 5] as const;

@Component({
  selector: 'yn-star-rating',
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './star-rating.component.html',
  styleUrl: './star-rating.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StarRatingComponent {
  rating = input<number | null>(null);
  readonly = input<boolean>(false);

  ratingChange = output<number>();

  protected readonly stars = STARS;
  protected readonly hovered = signal<number | null>(null);

  protected readonly highlight = computed(() => this.hovered() ?? this.rating() ?? 0);

  private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('star');

  protected onClick(value: number): void {
    if (this.readonly()) return;

    this.ratingChange.emit(value);

    if (!prefersReducedMotion()) {
      const button = this.buttons()[value - 1]?.nativeElement;
      if (button) springPress(button, 0.85);
    }
  }

  protected onEnter(value: number): void {
    if (!this.readonly()) this.hovered.set(value);
  }

  protected onLeave(): void {
    this.hovered.set(null);
  }
}
