import { type Browser, type Page } from '@playwright/test';
import { KeycloakLoginPage } from '../pages/keycloak-login.page';
import { LoginPage } from '../pages/login.page';
import { TIMEOUTS } from './timeouts';

const STORAGE_STATE_PATH = 'src/.auth/user.json';
const GATEWAY_URL = process.env['GATEWAY_URL'] ?? 'http://localhost:5100';

/**
 * Test constants for E2E tests against the real system.
 * The test user is pre-seeded in the Keycloak realm import.
 */
export const TEST_USER = {
  username: process.env['E2E_USER'] ?? 'testuser',
  password: process.env['E2E_PASSWORD'] ?? 'Test1234',
  email: 'test@yumney.dev',
  displayName: 'Test User',
};

export function uniqueTitle(prefix: string): string {
  // Include a random suffix so two specs calling uniqueTitle in the same
  // millisecond (possible with parallel workers) produce distinct values.
  return `${prefix} ${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

/**
 * Open an authenticated page in a fresh context that reuses the storageState
 * planted by auth.setup. Use in beforeAll/afterAll blocks that need a short
 * authenticated session for test data setup or cleanup — cheaper and more
 * reliable than running `loginViaKeycloak` which replays the brittle UI
 * redirect flow.
 */
export async function openAuthenticatedPage(browser: Browser): Promise<Page> {
  const context = await browser.newContext({ storageState: STORAGE_STATE_PATH });
  const page = await context.newPage();
  // Navigate to a tiny static asset on the :4200 origin so the page is
  // on-origin (createTestRecipe reads access_token from localStorage and
  // does relative fetches against the gateway). Avoids the 5–10s federation
  // cold-start that `page.goto('/')` triggers in dev mode — beforeAll hooks
  // were timing out at 60s when 30s+ was burned on Angular bootstrap before
  // a single API call could fire.
  await page.goto('/assets/config/app-config.json', { waitUntil: 'domcontentloaded' });
  return page;
}

export function uniqueEmail(prefix = 'e2e'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2)}@yumney.dev`;
}

/**
 * Authenticate via the Keycloak login form.
 * Use this in beforeAll blocks where the auth fixture is not available.
 */
export async function loginViaKeycloak(
  page: Page,
  username: string = TEST_USER.username,
  password: string = TEST_USER.password,
): Promise<void> {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.signInButton.click();
  await page.waitForURL('**/realms/yumney/protocol/openid-connect/**');
  const keycloak = new KeycloakLoginPage(page);
  await keycloak.signIn(username, password);
  await page.waitForURL('**/dashboard', { timeout: TIMEOUTS.long });
}

/**
 * Create a test recipe via the API (POST /api/v1/recipes) and return its
 * identifier. Runs inside the browser context so localStorage tokens are in
 * scope for the Authorization header. Targets the Gateway (:5100) directly,
 * not the dev server (:4200) — the dev server doesn't proxy /api/* so
 * relative fetches from there 404. ~1s vs ~15-25s for the UI-form
 * equivalent, and robust under parallel worker pressure.
 */
export async function createTestRecipe(
  page: Page,
  title: string,
  options?: { ingredient?: string; step?: string; servings?: number; tags?: string[] },
): Promise<string> {
  const ingredient = options?.ingredient ?? 'Test Ingredient';
  const step = options?.step ?? 'Test Step';
  const servings = options?.servings ?? 4;
  const tags = options?.tags ?? [];

  const identifier = await page.evaluate(
    async ({ title, ingredient, step, servings, tags, gatewayUrl }) => {
      const token = localStorage.getItem('access_token');
      const res = await fetch(`${gatewayUrl}/api/v1/recipes`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify({
          title,
          description: null,
          ingredients: [{ name: ingredient, amount: 1, unit: 'piece' }],
          steps: [{ number: 1, description: step }],
          servings,
          prepTimeMinutes: null,
          cookTimeMinutes: null,
          difficulty: null,
          imageUrl: null,
          tags,
        }),
      });
      if (!res.ok) {
        throw new Error(`createTestRecipe failed: ${res.status} ${await res.text()}`);
      }
      const body = (await res.json()) as { identifier: string };
      return body.identifier;
    },
    { title, ingredient, step, servings, tags, gatewayUrl: GATEWAY_URL },
  );

  return identifier;
}

/**
 * Assign a recipe to a meal-plan slot via the API (POST
 * /api/v1/meal-plans/{year}/w/{week}/slots). Returns the year/week the slot
 * was assigned to so tests can navigate the planner to that exact week.
 */
export async function assignMealSlot(
  page: Page,
  options: {
    year: number;
    weekNumber: number;
    day: string;
    recipeIdentifier: string;
    recipeTitle: string;
    mealType?: string;
  },
): Promise<void> {
  await page.evaluate(
    async ({ assignOptions, gatewayUrl }) => {
      const token = localStorage.getItem('access_token');
      const res = await fetch(
        `${gatewayUrl}/api/v1/meal-plans/${assignOptions.year}/w/${assignOptions.weekNumber}/slots`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            day: assignOptions.day,
            recipeIdentifier: assignOptions.recipeIdentifier,
            recipeTitle: assignOptions.recipeTitle,
            mealType: assignOptions.mealType ?? 'Dinner',
          }),
        },
      );
      if (!res.ok) {
        throw new Error(`assignMealSlot failed: ${res.status} ${await res.text()}`);
      }
    },
    { assignOptions: options, gatewayUrl: GATEWAY_URL },
  );
}

/**
 * Delete a recipe via the API. Mirror of createTestRecipe for cleanup in
 * afterAll blocks.
 */
export async function deleteTestRecipe(page: Page, recipeIdentifier: string): Promise<void> {
  await page.evaluate(
    async ({ id, gatewayUrl }) => {
      const token = localStorage.getItem('access_token');
      const res = await fetch(`${gatewayUrl}/api/v1/recipes/${id}`, {
        method: 'DELETE',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok && res.status !== 404) {
        throw new Error(`deleteTestRecipe failed: ${res.status} ${await res.text()}`);
      }
    },
    { id: recipeIdentifier, gatewayUrl: GATEWAY_URL },
  );
}
