import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';

test.describe('Login Page', () => {
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
    await expect(loginPage.forgotPasswordLink).toHaveText(/Forgot your password/);
  });

  test('should have correct page title', async ({ page }) => {
    await expect(page).toHaveTitle(/Sign In/);
  });
});
