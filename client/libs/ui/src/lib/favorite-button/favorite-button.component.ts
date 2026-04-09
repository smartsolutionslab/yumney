import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

@Component({
  selector: 'yn-favorite-button',
  standalone: true,
  imports: [TranslocoModule],
  template: `
    <button
      type="button"
      class="favorite-button"
      [class.is-favorite]="isFavorite()"
      [attr.aria-pressed]="isFavorite()"
      [attr.aria-label]="
        isFavorite() ? t('recipes.favorite.removeAriaLabel') : t('recipes.favorite.addAriaLabel')
      "
      (click)="onClick($event)"
      *transloco="let t"
    >
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <path
          d="M12 21s-7-4.5-9.3-9C1.4 9.4 2.5 6 5.6 5c2-.6 4 .3 5 1.9.4.6.6 1.2.6 1.2s.2-.6.6-1.2c1-1.6 3-2.5 5-1.9 3.1 1 4.2 4.4 2.9 7-2.3 4.5-9.3 9-9.3 9z"
          [attr.fill]="isFavorite() ? 'currentColor' : 'none'"
          stroke="currentColor"
          stroke-width="2"
          stroke-linejoin="round"
        />
      </svg>
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
