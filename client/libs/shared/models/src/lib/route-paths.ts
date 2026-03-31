export const ROUTES = {
  dashboard: '/dashboard',
  auth: {
    login: '/auth/login',
    register: '/auth/register',
    resendVerification: '/auth/resend-verification',
  },
  recipes: {
    list: '/recipes',
    detail: (identifier: string) => `/recipes/${identifier}`,
    edit: (identifier: string) => `/recipes/${identifier}/edit`,
  },
  shopping: {
    list: '/shopping',
    detail: (identifier: string) => `/shopping/${identifier}`,
    create: (recipeId: string) => `/shopping/create/${recipeId}`,
  },
} as const;
