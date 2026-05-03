import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { RecipeListItem } from '../../api';
import { ROUTES } from '@yumney/shared/models';
import { FavoriteButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-card',
  imports: [TranslocoModule, RouterLink, LucideAngularModule, FavoriteButtonComponent],
  templateUrl: './recipe-card.component.html',
  styleUrl: './recipe-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
  protected readonly ROUTES = ROUTES;

  recipe = input.required<RecipeListItem>();
  assignMode = input(false);
  multiSelectMode = input(false);
  selected = input(false);
  toggleFavorite = output<string>();
  assign = output<RecipeListItem>();
  toggleSelect = output<string>();

  protected onToggleFavorite(): void {
    this.toggleFavorite.emit(this.recipe().identifier);
  }

  protected onAssign(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.assign.emit(this.recipe());
  }

  protected onToggleSelect(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.toggleSelect.emit(this.recipe().identifier);
  }
}
