export const mockRecipeDetail = {
  identifier: 'recipe-e2e-001',
  title: 'Pasta Carbonara',
  description: 'A classic Italian pasta dish with eggs and bacon',
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
  sourceUrl: 'https://example.com/carbonara',
  createdAt: '2026-03-10T00:00:00Z',
  ingredients: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Pancetta', amount: 200, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
    { name: 'Parmesan', amount: 100, unit: 'g' },
    { name: 'Black Pepper', amount: null, unit: null },
  ],
  steps: [
    { number: 1, description: 'Cook spaghetti in salted boiling water' },
    { number: 2, description: 'Fry pancetta until crispy' },
    { number: 3, description: 'Mix eggs with grated parmesan' },
    { number: 4, description: 'Combine pasta with pancetta, remove from heat, add egg mixture' },
  ],
};

export const mockRecipeList = {
  items: [
    {
      identifier: 'recipe-e2e-001',
      title: 'Pasta Carbonara',
      description: 'A classic Italian pasta dish',
      servings: 4,
      prepTimeMinutes: 10,
      cookTimeMinutes: 20,
      difficulty: 'medium',
      imageUrl: null,
      createdAt: '2026-03-10T00:00:00Z',
    },
    {
      identifier: 'recipe-e2e-002',
      title: 'Chicken Tikka Masala',
      description: 'Creamy Indian curry with tender chicken',
      servings: 6,
      prepTimeMinutes: 20,
      cookTimeMinutes: 30,
      difficulty: 'medium',
      imageUrl: null,
      createdAt: '2026-03-09T00:00:00Z',
    },
    {
      identifier: 'recipe-e2e-003',
      title: 'Caesar Salad',
      description: 'Classic salad with romaine and croutons',
      servings: 2,
      prepTimeMinutes: 15,
      cookTimeMinutes: null,
      difficulty: 'easy',
      imageUrl: null,
      createdAt: '2026-03-08T00:00:00Z',
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 3,
  totalPages: 1,
};

export const mockImportResponse = {
  title: 'Pasta Carbonara',
  description: 'A classic Italian pasta dish with eggs and bacon',
  ingredients: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
    { name: 'Parmesan', amount: 100, unit: 'g' },
  ],
  steps: [
    { number: 1, description: 'Cook pasta' },
    { number: 2, description: 'Mix eggs and cheese' },
  ],
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
};

export const mockSavedRecipeResponse = {
  identifier: 'recipe-e2e-new',
  title: 'Pasta Carbonara',
  createdAt: '2026-03-15T00:00:00Z',
};

export const mockShoppingListDetail = {
  identifier: 'list-e2e-001',
  title: 'Pasta Carbonara',
  recipeIdentifier: 'recipe-e2e-001',
  createdAt: '2026-03-10T00:00:00Z',
  items: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Pancetta', amount: 200, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
  ],
};

export const mockShoppingLists = [
  { identifier: 'list-e2e-001', title: 'Pasta Carbonara', itemCount: 5, createdAt: '2026-03-10T00:00:00Z' },
  { identifier: 'list-e2e-002', title: 'Weekly Groceries', itemCount: 12, createdAt: '2026-03-11T00:00:00Z' },
];
