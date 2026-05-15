import { computed, inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { createAsyncState, ERROR_MAPS, ROUTES } from '@yumney/shared/models';
import { MealPlanApiService, RecipeListItem } from '../api';

const ASSIGN_TO_PATTERN = /^(\d{4})-W(\d{1,2})-(\w+)$/;

@Injectable()
export class RecipeAssignmentService {
  private mealPlanApi = inject(MealPlanApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private assignState = createAsyncState();

  readonly assignTo = signal<string | null>(null);
  readonly assignMode = computed(() => this.assignTo() !== null);
  readonly assigning = this.assignState.isLoading;
  readonly serverError = this.assignState.serverError;

  initFromRoute(): void {
    this.assignTo.set(this.route.snapshot.queryParamMap.get('assignTo'));
  }

  assign(recipe: RecipeListItem): void {
    const raw = this.assignTo();
    if (!raw) return;

    const match = raw.match(ASSIGN_TO_PATTERN);
    if (!match) return;

    const [, yearStr, weekStr, day] = match;

    this.assignState.execute(
      this.mealPlanApi.assignRecipe(Number(yearStr), Number(weekStr), {
        day,
        recipeIdentifier: recipe.identifier,
        recipeTitle: recipe.title,
      }),
      ERROR_MAPS.mealPlanner.assign,
      () => this.router.navigate([ROUTES.mealPlanner]),
    );
  }

  cancel(): void {
    void this.router.navigate([ROUTES.mealPlanner]);
  }
}
