import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { MealPlanApiService, RecipeListItem } from '../api';
import { ROUTES } from '@yumney/shared/models';

const ASSIGN_TO_PATTERN = /^(\d{4})-W(\d{1,2})-(\w+)$/;

@Injectable()
export class RecipeAssignmentService {
  private mealPlanApi = inject(MealPlanApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly assignTo = signal<string | null>(null);
  readonly assignMode = computed(() => this.assignTo() !== null);
  readonly assigning = signal(false);

  initFromRoute(): void {
    this.assignTo.set(this.route.snapshot.queryParamMap.get('assignTo'));
  }

  assign(recipe: RecipeListItem): void {
    const raw = this.assignTo();
    if (!raw) return;

    const match = raw.match(ASSIGN_TO_PATTERN);
    if (!match) return;

    const [, yearStr, weekStr, day] = match;
    this.assigning.set(true);

    this.mealPlanApi
      .assignRecipe(Number(yearStr), Number(weekStr), {
        day,
        recipeIdentifier: recipe.identifier,
        recipeTitle: recipe.title,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.router.navigate([ROUTES.mealPlanner]),
        error: () => this.assigning.set(false),
      });
  }

  cancel(): void {
    this.router.navigate([ROUTES.mealPlanner]);
  }
}
