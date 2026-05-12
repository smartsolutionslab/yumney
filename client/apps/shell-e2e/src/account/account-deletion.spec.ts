import { test, expect } from '../fixtures/auth.fixture';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * GDPR Art. 17 — danger-zone delete flow (US-101).
 *
 * The backend cascade (account purge across all 4 modules) is covered by
 * AccountDeletionCascadeTests / DangerZoneEndpointsContractTests in the
 * Integration.Tests project. This spec only verifies the UI wiring:
 *
 *   - the button starts disabled
 *   - typing anything other than "DELETE" keeps it disabled
 *   - typing "DELETE" enables it
 *   - clicking calls DELETE /api/v1/users/me and logs the user out
 *
 * The DELETE call is mocked so the shared `testuser` account stays usable
 * for every other E2E spec. Cross-origin route on `context.route` (gateway
 * on :5100, shell on :4200) — see error-mock.ts and the project memory.
 */
test.describe('Danger Zone — account deletion (US-101)', () => {
  test('disables the delete button until "DELETE" is typed', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account/danger-zone');

    const deleteButton = authenticatedPage.getByTestId('delete-account-btn');
    const confirmInput = authenticatedPage.locator('.danger-zone__field input');

    await expect(deleteButton).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(deleteButton).toBeDisabled();

    await confirmInput.fill('delete'); // case-sensitive
    await expect(deleteButton).toBeDisabled();

    await confirmInput.fill('not the token');
    await expect(deleteButton).toBeDisabled();

    await confirmInput.fill('DELETE');
    await expect(deleteButton).toBeEnabled();
  });

  test('clicking delete calls the API and logs the user out', async ({ authenticatedPage }) => {
    let deleteCalled = false;
    await authenticatedPage.context().route('**/api/v1/users/me', (route) => {
      if (route.request().method() === 'DELETE') {
        deleteCalled = true;
        return route.fulfill({ status: 204 });
      }

      return route.continue();
    });

    await authenticatedPage.goto('/account/danger-zone');
    const confirmInput = authenticatedPage.locator('.danger-zone__field input');
    await confirmInput.fill('DELETE');

    await authenticatedPage.getByTestId('delete-account-btn').click();

    // Logout redirects away from the account MFE — Keycloak's end-session
    // endpoint or the shell's login route, depending on AuthService config.
    // We only assert that we've left the danger zone.
    await authenticatedPage.waitForURL(
      (url) => !url.pathname.includes('/account/danger-zone'),
      { timeout: TIMEOUTS.long },
    );
    expect(deleteCalled).toBe(true);
  });
});
