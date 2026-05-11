import { HttpErrorMap } from './http-error-utils';

export const ERROR_MAPS = {
  dashboard: {
    import: {
      502: 'dashboard.import.errors.unreachable',
      504: 'dashboard.import.errors.timeout',
      404: 'dashboard.import.errors.noRecipe',
      default: 'dashboard.import.errors.generic',
    } satisfies HttpErrorMap,
    save: {
      409: 'dashboard.save.errors.duplicate',
      default: 'dashboard.save.errors.generic',
    } satisfies HttpErrorMap,
  },
  recipes: {
    detail: {
      404: 'recipes.detail.notFound',
      default: 'recipes.detail.errors.generic',
    } satisfies HttpErrorMap,
    delete: {
      404: 'recipes.detail.delete.errors.notFound',
      default: 'recipes.detail.delete.errors.generic',
    } satisfies HttpErrorMap,
    edit: {
      404: 'recipes.edit.errors.notFound',
      default: 'recipes.edit.errors.generic',
    } satisfies HttpErrorMap,
    list: {
      default: 'recipes.list.errors.generic',
    } satisfies HttpErrorMap,
    cookable: {
      default: 'recipes.cookable.errors.generic',
    } satisfies HttpErrorMap,
    createShoppingList: {
      default: 'recipes.detail.createShoppingList.errors.generic',
    } satisfies HttpErrorMap,
  },
  shopping: {
    list: {
      default: 'shopping.list.errors.generic',
    } satisfies HttpErrorMap,
    detail: {
      404: 'shopping.detail.errors.notFound',
      default: 'shopping.detail.errors.generic',
    } satisfies HttpErrorMap,
    createLoad: {
      404: 'shopping.create.errors.recipeNotFound',
      default: 'shopping.create.errors.generic',
    } satisfies HttpErrorMap,
    create: {
      default: 'shopping.create.errors.createFailed',
    } satisfies HttpErrorMap,
    merged: {
      add: {
        default: 'shopping.errors.addFailed',
      } satisfies HttpErrorMap,
      remove: {
        default: 'shopping.errors.removeFailed',
      } satisfies HttpErrorMap,
      export: {
        default: 'shopping.errors.exportFailed',
      } satisfies HttpErrorMap,
    },
    pantry: {
      load: {
        default: 'shopping.pantry.errors.loadFailed',
      } satisfies HttpErrorMap,
      freeze: {
        default: 'shopping.pantry.errors.freezeFailed',
      } satisfies HttpErrorMap,
    },
  },
  mealPlanner: {
    assign: {
      default: 'mealPlanner.errors.assignFailed',
    } satisfies HttpErrorMap,
    load: {
      default: 'mealPlanner.errors.loadFailed',
    } satisfies HttpErrorMap,
    clearSlot: {
      default: 'mealPlanner.errors.clearFailed',
    } satisfies HttpErrorMap,
    generateShoppingList: {
      default: 'mealPlanner.errors.generateFailed',
    } satisfies HttpErrorMap,
    copyToWeek: {
      404: 'mealPlanner.history.errors.sourceNotFound',
      422: 'mealPlanner.history.errors.copyValidation',
      default: 'mealPlanner.history.errors.copyFailed',
    } satisfies HttpErrorMap,
    searchHistory: {
      default: 'mealPlanner.history.errors.searchFailed',
    } satisfies HttpErrorMap,
    suggestWeek: {
      422: 'mealPlanner.suggest.errors.noRecipes',
      default: 'mealPlanner.suggest.errors.failed',
    } satisfies HttpErrorMap,
  },
  account: {
    load: {
      default: 'account.settings.errors.loadFailed',
    } satisfies HttpErrorMap,
    save: {
      default: 'account.settings.errors.saveFailed',
    } satisfies HttpErrorMap,
  },
  auth: {
    register: {
      409: 'auth.register.errors.emailAlreadyExists',
      422: 'auth.register.errors.validationFailed',
      default: 'auth.register.errors.generic',
    } satisfies HttpErrorMap,
    resendVerification: {
      503: 'auth.resendVerification.errors.serviceUnavailable',
      default: 'auth.resendVerification.errors.generic',
    } satisfies HttpErrorMap,
  },
} as const;
