import {
  Component,
  ChangeDetectionStrategy,
  HostListener,
  computed,
  input,
  output,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import type { ScalableIngredient } from '@yumney/shared/models';

@Component({
  selector: 'yn-create-shopping-list-dialog',
  imports: [TranslocoModule],
  templateUrl: './create-shopping-list-dialog.component.html',
  styleUrl: './create-shopping-list-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateShoppingListDialogComponent {
  recipeTitle = input.required<string>();
  desiredServings = input.required<number>();
  ingredients = input.required<readonly ScalableIngredient[]>();
  isCreating = input(false);

  confirmed = output<void>();
  cancelled = output<void>();

  suggestedTitle = computed(() => `${this.recipeTitle()} (x${this.desiredServings()})`);

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (!this.isCreating()) {
      this.cancelled.emit();
    }
  }

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.isCreating()) {
      this.cancelled.emit();
    }
  }
}
