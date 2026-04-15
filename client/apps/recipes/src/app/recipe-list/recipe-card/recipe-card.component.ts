import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeListItem } from '@yumney/shared/api-client';
import { ROUTES } from '@yumney/shared/models';
import { FavoriteButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-card',
  imports: [TranslocoModule, RouterLink, FavoriteButtonComponent],
  templateUrl: './recipe-card.component.html',
  styleUrl: './recipe-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
  protected readonly ROUTES = ROUTES;

  recipe = input.required<RecipeListItem>();
  toggleFavorite = output<string>();

  protected onToggleFavorite(): void {
    this.toggleFavorite.emit(this.recipe().identifier);
  }
}
