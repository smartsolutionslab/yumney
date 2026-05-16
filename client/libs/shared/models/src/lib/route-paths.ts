export const ROUTES = {
  dashboard: '/dashboard',
  auth: {
    login: '/auth/login',
    register: '/auth/register',
    resendVerification: '/auth/resend-verification',
  },
  recipes: {
    list: '/recipes',
    cookable: '/recipes/cookable',
    detail: (identifier: string) => `/recipes/${identifier}`,
    edit: (identifier: string) => `/recipes/${identifier}/edit`,
    cook: (identifier: string) => `/recipes/${identifier}/cook`,
  },
  mealPlanner: '/meal-planner',
  shopping: {
    list: '/shopping',
    pantry: '/shopping/pantry',
    detail: (identifier: string) => `/shopping/lists/${identifier}`,
    create: (recipeId: string) => `/shopping/create/${recipeId}`,
  },
  docs: {
    mcp: '/docs/mcp',
  },
} as const;
