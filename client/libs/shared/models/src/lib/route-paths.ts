export const ROUTES = {
  dashboard: '/dashboard',
  auth: {
    login: '/auth/login',
  },
  recipes: {
    list: '/recipes',
    detail: (identifier: string) => `/recipes/${identifier}`,
  },
  shopping: {
    list: '/shopping',
    detail: (identifier: string) => `/shopping/${identifier}`,
  },
} as const;
