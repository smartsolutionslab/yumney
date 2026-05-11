const API_BASE = '/api/v1';

export const API_ENDPOINTS = {
  recipes: {
    base: `${API_BASE}/recipes`,
    import: `${API_BASE}/recipes/import`,
    importStream: (url: string) =>
      `${API_BASE}/recipes/import/stream?url=${encodeURIComponent(url)}`,
    importFromPhotos: `${API_BASE}/recipes/import-from-photos`,
    recognizeIngredients: `${API_BASE}/recipes/recognize-ingredients`,
    chat: `${API_BASE}/recipes/chat`,
    parseIntent: `${API_BASE}/recipes/parse-intent`,
    importFromText: `${API_BASE}/recipes/import-from-text`,
    byIdentifier: (identifier: string) => `${API_BASE}/recipes/${identifier}`,
    favorite: (identifier: string) => `${API_BASE}/recipes/${identifier}/favorite`,
    cooked: (identifier: string) => `${API_BASE}/recipes/${identifier}/cooked`,
    rating: (identifier: string) => `${API_BASE}/recipes/${identifier}/rating`,
    notes: (identifier: string) => `${API_BASE}/recipes/${identifier}/notes`,
    whatCanICook: `${API_BASE}/recipes/what-can-i-cook`,
  },
  shoppingLists: {
    base: `${API_BASE}/shopping-lists`,
    fromRecipes: `${API_BASE}/shopping-lists/from-recipes`,
    byIdentifier: (identifier: string) => `${API_BASE}/shopping-lists/${identifier}`,
    checkItem: (listIdentifier: string, itemIdentifier: string) =>
      `${API_BASE}/shopping-lists/${listIdentifier}/items/${itemIdentifier}/check`,
    itemCategory: (listIdentifier: string, itemIdentifier: string) =>
      `${API_BASE}/shopping-lists/${listIdentifier}/items/${itemIdentifier}/category`,
    checkAll: (listIdentifier: string) => `${API_BASE}/shopping-lists/${listIdentifier}/check-all`,
    merged: `${API_BASE}/shopping-lists/merged`,
    items: `${API_BASE}/shopping-lists/items`,
    itemsFreeze: `${API_BASE}/shopping-lists/items/freeze`,
    balance: `${API_BASE}/shopping-lists/balance`,
    export: `${API_BASE}/shopping-lists/export`,
    shoppingModeStart: `${API_BASE}/shopping-lists/shopping-mode/start`,
    shoppingModeEnd: `${API_BASE}/shopping-lists/shopping-mode/end`,
  },
  mealPlans: {
    byWeek: (year: number, week: number) => `${API_BASE}/meal-plans/${year}/w/${week}`,
    slots: (year: number, week: number) => `${API_BASE}/meal-plans/${year}/w/${week}/slots`,
    slotsSwap: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/slots/swap`,
    slotsServings: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/slots/servings`,
    slotsConfirm: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/slots/confirm`,
    extendedMode: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/extended-mode`,
    cookWithLeftovers: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/cook-with-leftovers`,
    plannedRecipes: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/planned-recipes`,
    generateShoppingList: (year: number, week: number) =>
      `${API_BASE}/meal-plans/${year}/w/${week}/generate-shopping-list`,
    historySearch: `${API_BASE}/meal-plans/history/search`,
    copyTo: (srcYear: number, srcWeek: number, dstYear: number, dstWeek: number) =>
      `${API_BASE}/meal-plans/${srcYear}/w/${srcWeek}/copy-to/${dstYear}/${dstWeek}`,
    suggest: (year: number, week: number) => `${API_BASE}/meal-plans/${year}/w/${week}/suggest`,
  },
  auth: {
    register: `${API_BASE}/auth/register`,
    resendVerificationEmail: `${API_BASE}/auth/resend-verification-email`,
  },
  users: {
    activity: `${API_BASE}/users/me/activity`,
    activityRecipeStats: (identifier: string) =>
      `${API_BASE}/users/me/activity/recipes/${identifier}/stats`,
    suggestions: `${API_BASE}/users/me/suggestions`,
    profile: `${API_BASE}/users/me/profile`,
    me: `${API_BASE}/users/me`,
  },
} as const;
