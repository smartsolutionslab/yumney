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
import { ButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-create-shopping-list-dialog',
  imports: [TranslocoModule, ButtonComponent],
  templateUrl: './create-shopping-list-dialog.component.html',
  styleUrl: './create-shopping-list-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateShoppingListDialogComponent {
  recipeTitle = input.required<string>();
  desiredServings = input.required<number | null>();
  ingredients = input.required<readonly ScalableIngredient[]>();
  isCreating = input(false);

  confirmed = output<void>();
  cancelled = output<void>();

  suggestedTitle = computed(() => {
    const servings = this.desiredServings();
    return servings === null ? this.recipeTitle() : `${this.recipeTitle()} (x${servings})`;
  });

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
