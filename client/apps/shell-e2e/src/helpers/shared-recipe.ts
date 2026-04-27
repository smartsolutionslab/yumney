import { type TestType } from '@playwright/test';
import {
  uniqueTitle,
  openAuthenticatedPage,
  createTestRecipe,
  deleteTestRecipe,
} from './test-data.helper';

export interface SharedRecipe {
  title: string;
  identifier: string;
}

type CreateRecipeOptions = Parameters<typeof createTestRecipe>[2];

/**
 * Registers a per-file shared recipe via beforeAll + afterAll on the supplied
 * test object, and returns a getter the surrounding tests use to read the
 * created identifier/title.
 *
 * Why this helper exists: the previous pattern in recipes/ and shopping/
 * specs was a hand-rolled `let recipeIdentifier` + beforeAll + afterAll +
 * `test.skip(!recipeIdentifier)` guard on every test. The skip guards were
 * dead code — `createTestRecipe` already throws on failure and Playwright
 * fails downstream tests with the beforeAll error — but they obscured intent
 * and let real "silently undefined" failure modes hide. Centralising the
 * boilerplate also wraps the teardown in try/finally so the auth context is
 * closed even when DELETE returns non-404.
 *
 * Per-file (not per-test) is intentional: the existing specs treat the
 * recipe as read-only fixture data and creating one per test would multiply
 * API churn ~6x for no isolation benefit (most tests don't mutate it).
 *
 * Note: this still uses afterAll for cleanup. A true Playwright fixture with
 * auto-teardown that survives test crashes is tracked under #406.
 */
export function setupSharedRecipe(
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  test: TestType<any, any>,
  prefix: string,
  options?: CreateRecipeOptions,
): () => SharedRecipe {
  let recipe: SharedRecipe | undefined;
  let setupPage: Awaited<ReturnType<typeof openAuthenticatedPage>> | undefined;

  test.beforeAll(async ({ browser }) => {
    setupPage = await openAuthenticatedPage(browser);
    const title = uniqueTitle(prefix);
    const identifier = await createTestRecipe(setupPage, title, options);
    recipe = { title, identifier };
  });

  test.afterAll(async () => {
    if (!setupPage) return;
    try {
      if (recipe?.identifier) {
        await deleteTestRecipe(setupPage, recipe.identifier);
      }
    } finally {
      await setupPage.context().close();
    }
  });

  return () => {
    if (!recipe) {
      throw new Error(
        'setupSharedRecipe: recipe accessed before beforeAll completed. ' +
          'Either the test ran out of order or beforeAll silently no-opped.',
      );
    }
    return recipe;
  };
}
