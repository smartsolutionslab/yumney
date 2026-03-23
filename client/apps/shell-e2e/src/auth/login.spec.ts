import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { TEST_USER } from '../helpers/test-data.helper';

test.describe('Login Page (US-002)', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
  });

  test('should display login heading and subtitle', async () => {
    await expect(loginPage.heading).toHaveText('Welcome back');
    await expect(loginPage.subtitle).toHaveText('Sign in to your Yumney account');
  });

  test('should display sign in button', async () => {
    await expect(loginPage.signInButton).toBeVisible();
    await expect(loginPage.signInButton).toHaveText(/Sign in with Keycloak/i);
  });

  test('should display remember me checkbox unchecked by default', async () => {
    await expect(loginPage.rememberMeCheckbox).toBeVisible();
    await expect(loginPage.rememberMeCheckbox).not.toBeChecked();
  });

  test('should toggle remember me checkbox', async () => {
    await loginPage.rememberMeCheckbox.check();
    await expect(loginPage.rememberMeCheckbox).toBeChecked();

    await loginPage.rememberMeCheckbox.uncheck();
    await expect(loginPage.rememberMeCheckbox).not.toBeChecked();
  });

  test('should navigate to register page via link', async ({ page }) => {
    await loginPage.registerLink.click();
    await expect(page).toHaveURL(/\/auth\/register/);
  });

  test('should display forgot password link', async () => {
    await expect(loginPage.forgotPasswordLink).toBeVisible();
  });

  test('should have correct page title', async ({ page }) => {
    await expect(page).toHaveTitle(/Sign In/);
  });

  test('should redirect to Keycloak on sign in click', async ({ page }) => {
    await loginPage.signInButton.click();
    await expect(page).toHaveURL(/realms\/yumney\/protocol\/openid-connect/);
  });

  test('should complete full login flow and reach dashboard', async ({ page }) => {
    await loginPage.signInButton.click();

    // Keycloak login page
    await page.locator('#username').fill(TEST_USER.username);
    await page.locator('#password').fill(TEST_USER.password);
    await page.locator('#kc-login').click();

    // Should arrive at dashboard
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 15_000 });
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  });
});
