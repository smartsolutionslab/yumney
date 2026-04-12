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
  },
  shoppingLists: {
    base: `${API_BASE}/shopping-lists`,
    byIdentifier: (identifier: string) => `${API_BASE}/shopping-lists/${identifier}`,
    checkItem: (listIdentifier: string, itemIdentifier: string) =>
      `${API_BASE}/shopping-lists/${listIdentifier}/items/${itemIdentifier}/check`,
    checkAll: (listIdentifier: string) => `${API_BASE}/shopping-lists/${listIdentifier}/check-all`,
    merged: `${API_BASE}/shopping-lists/merged`,
    items: `${API_BASE}/shopping-lists/items`,
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
  },
  auth: {
    register: `${API_BASE}/auth/register`,
    resendVerificationEmail: `${API_BASE}/auth/resend-verification-email`,
  },
  users: {
    activity: `${API_BASE}/users/me/activity`,
    suggestions: `${API_BASE}/users/me/suggestions`,
    profile: `${API_BASE}/users/me/profile`,
  },
} as const;
