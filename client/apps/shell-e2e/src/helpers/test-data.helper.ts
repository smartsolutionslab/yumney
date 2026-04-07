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
  return `${prefix} ${Date.now()}`;
}
