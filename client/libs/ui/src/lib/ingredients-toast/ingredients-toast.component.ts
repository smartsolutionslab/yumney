import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import type { RecognizedIngredient } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-ingredients-toast',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './ingredients-toast.component.html',
  styleUrl: './ingredients-toast.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IngredientsToastComponent {
  ingredients = input.required<RecognizedIngredient[]>();
  dismissed = output<void>();
}
