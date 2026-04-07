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
    byIdentifier: (identifier: string) => `${API_BASE}/recipes/${identifier}`,
  },
  shoppingLists: {
    base: `${API_BASE}/shopping-lists`,
    byIdentifier: (identifier: string) => `${API_BASE}/shopping-lists/${identifier}`,
    checkItem: (listIdentifier: string, itemIdentifier: string) =>
      `${API_BASE}/shopping-lists/${listIdentifier}/items/${itemIdentifier}/check`,
    checkAll: (listIdentifier: string) => `${API_BASE}/shopping-lists/${listIdentifier}/check-all`,
  },
  auth: {
    register: `${API_BASE}/auth/register`,
    resendVerificationEmail: `${API_BASE}/auth/resend-verification-email`,
  },
  users: {
    activity: `${API_BASE}/users/me/activity`,
    suggestions: `${API_BASE}/users/me/suggestions`,
  },
} as const;
