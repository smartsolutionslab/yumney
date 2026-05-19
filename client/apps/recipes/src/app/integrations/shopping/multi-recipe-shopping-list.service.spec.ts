import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { RecipeApiService, ShoppingApiService } from '../../api';
import { MultiRecipeShoppingListService } from './multi-recipe-shopping-list.service';

interface ParamSnapshot {
  multiSelect?: string;
  preselect?: string;
}

function createService(params: ParamSnapshot = {}): MultiRecipeShoppingListService {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [setupTranslocoTesting({})],
    providers: [
      MultiRecipeShoppingListService,
      { provide: RecipeApiService, useValue: { getRecipeById: vi.fn() } },
      { provide: ShoppingApiService, useValue: { createShoppingListFromRecipes: vi.fn() } },
      { provide: Router, useValue: { navigateByUrl: vi.fn() } },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            queryParamMap: {
              get: (key: string) => (params as Record<string, string | undefined>)[key] ?? null,
            },
          },
        },
      },
    ],
  });

  return TestBed.inject(MultiRecipeShoppingListService);
}

describe('MultiRecipeShoppingListService.initFromRoute', () => {
  it('does nothing when multiSelect is absent', () => {
    const service = createService();

    service.initFromRoute();

    expect(service.multiSelectMode()).toBe(false);
    expect(service.selectedRecipeIds().size).toBe(0);
  });

  it('does nothing when multiSelect is present but not "true"', () => {
    const service = createService({ multiSelect: 'false' });

    service.initFromRoute();

    expect(service.multiSelectMode()).toBe(false);
  });

  it('enters multi-select mode when multiSelect=true', () => {
    const service = createService({ multiSelect: 'true' });

    service.initFromRoute();

    expect(service.multiSelectMode()).toBe(true);
    expect(service.selectedRecipeIds().size).toBe(0);
  });

  it('preselects the comma-separated identifiers from the preselect param', () => {
    const service = createService({ multiSelect: 'true', preselect: 'abc,def,ghi' });

    service.initFromRoute();

    expect(service.multiSelectMode()).toBe(true);
    expect([...service.selectedRecipeIds()]).toEqual(['abc', 'def', 'ghi']);
  });

  it('trims whitespace and drops empty entries in the preselect csv', () => {
    const service = createService({ multiSelect: 'true', preselect: ' abc , , def ' });

    service.initFromRoute();

    expect([...service.selectedRecipeIds()]).toEqual(['abc', 'def']);
  });

  it('ignores the preselect param when multiSelect is not true', () => {
    const service = createService({ preselect: 'abc,def' });

    service.initFromRoute();

    expect(service.selectedRecipeIds().size).toBe(0);
  });
});
