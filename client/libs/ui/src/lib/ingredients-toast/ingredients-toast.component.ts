import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import type { RecognizedIngredient } from '@yumney/shared/api-recipes';

@Component({
  selector: 'yn-ingredients-toast',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './ingredients-toast.component.html',
  styleUrl: './ingredients-toast.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IngredientsToastComponent {
  ingredients = input.required<RecognizedIngredient[]>();
  dismissed = output<void>();
}
