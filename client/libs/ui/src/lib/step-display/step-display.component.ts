import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'yn-step-display',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './step-display.component.html',
  styleUrl: './step-display.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepDisplayComponent {
  stepNumber = input.required<number>();
  totalSteps = input.required<number>();
  text = input.required<string>();
}
