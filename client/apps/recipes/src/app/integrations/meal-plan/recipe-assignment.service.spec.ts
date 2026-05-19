import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { NEVER, of, throwError } from 'rxjs';
import { MealPlanApiService, RecipeListItem } from '../../api';
import { RecipeAssignmentService } from './recipe-assignment.service';

const recipe: RecipeListItem = {
  identifier: 'abc-123',
  title: 'Pasta Carbonara',
  description: null,
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: null,
  imageUrl: null,
  createdAt: '2026-03-10T00:00:00Z',
  tags: [],
  isFavorite: false,
};

describe('RecipeAssignmentService', () => {
  let assignRecipe: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.fn>;
  let assignToParam: string | null;

  const setup = (initialParam: string | null = null, apiResponse: ReturnType<typeof of> | ReturnType<typeof throwError> = of({})) => {
    assignToParam = initialParam;
    assignRecipe = vi.fn().mockReturnValue(apiResponse);
    navigate = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        RecipeAssignmentService,
        { provide: MealPlanApiService, useValue: { assignRecipe } },
        { provide: Router, useValue: { navigate } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => assignToParam } } },
        },
      ],
    });

    return TestBed.inject(RecipeAssignmentService);
  };

  it('should be inactive by default', () => {
    const service = setup();

    expect(service.assignMode()).toBe(false);
    expect(service.assignTo()).toBeNull();
  });

  it('should enter assign mode when route has an assignTo param', () => {
    const service = setup('2026-W15-monday');
    service.initFromRoute();

    expect(service.assignMode()).toBe(true);
    expect(service.assignTo()).toBe('2026-W15-monday');
  });

  it('should not call assignRecipe when not in assign mode', () => {
    const service = setup();

    service.assign(recipe);

    expect(assignRecipe).not.toHaveBeenCalled();
  });

  it('should not call assignRecipe when assignTo param is malformed', () => {
    const service = setup('totally-invalid');
    service.initFromRoute();

    service.assign(recipe);

    expect(assignRecipe).not.toHaveBeenCalled();
  });

  it('should parse year/week/day and forward recipe identity to the API', () => {
    const service = setup('2026-W15-monday');
    service.initFromRoute();

    service.assign(recipe);

    expect(assignRecipe).toHaveBeenCalledWith(2026, 15, {
      day: 'monday',
      recipeIdentifier: 'abc-123',
      recipeTitle: 'Pasta Carbonara',
    });
  });

  it('should set assigning true while the request is in flight', () => {
    const service = setup('2026-W1-tuesday', NEVER);
    service.initFromRoute();

    service.assign(recipe);

    expect(service.assigning()).toBe(true);
  });

  it('should navigate to the meal planner on success', () => {
    const service = setup('2026-W1-tuesday');
    service.initFromRoute();

    service.assign(recipe);

    expect(navigate).toHaveBeenCalledWith(['/meal-planner']);
  });

  it('should reset assigning on failure without navigating', () => {
    const service = setup(
      '2026-W1-tuesday',
      throwError(() => new Error('nope')),
    );
    service.initFromRoute();

    service.assign(recipe);

    expect(service.assigning()).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should navigate away when cancelled', () => {
    const service = setup('2026-W1-tuesday');
    service.initFromRoute();

    service.cancel();

    expect(navigate).toHaveBeenCalledWith(['/meal-planner']);
  });
});
