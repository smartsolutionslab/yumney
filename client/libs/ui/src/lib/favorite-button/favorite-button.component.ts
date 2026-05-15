import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'yn-favorite-button',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  template: `
    <button
      type="button"
      class="favorite-button"
      [class.is-favorite]="isFavorite()"
      [attr.aria-pressed]="isFavorite()"
      [attr.aria-label]="isFavorite() ? t('recipes.favorite.removeAriaLabel') : t('recipes.favorite.addAriaLabel')"
      (click)="onClick($event)"
      *transloco="let t"
    >
      <lucide-icon name="heart" [size]="20" />
    </button>
  `,
  styleUrl: './favorite-button.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FavoriteButtonComponent {
  isFavorite = input.required<boolean>();
  toggled = output<void>();

  protected onClick(event: Event): void {
    // Stop propagation so wrapping <a> elements (e.g., recipe card) don't navigate.
    event.stopPropagation();
    event.preventDefault();
    this.toggled.emit();
  }
}
