import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import type { CookingTimer } from '@yumney/shared/models';

const RADIUS = 60;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

@Component({
  selector: 'yn-cooking-timer',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './cooking-timer.component.html',
  styleUrl: './cooking-timer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookingTimerComponent {
  timer = input.required<CookingTimer>();
  cancel = output<string>();

  protected readonly radius = RADIUS;
  protected readonly circumference = CIRCUMFERENCE;

  protected readonly displayText = computed(() => {
    const t = this.timer();
    const minutes = Math.floor(t.remainingSeconds / 60);
    const seconds = t.remainingSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  });

  protected readonly dashOffset = computed(() => {
    const t = this.timer();
    if (t.totalSeconds === 0) return 0;
    const progress = t.remainingSeconds / t.totalSeconds;
    return CIRCUMFERENCE * (1 - progress);
  });

  protected onCancel(): void {
    this.cancel.emit(this.timer().id);
  }
}
