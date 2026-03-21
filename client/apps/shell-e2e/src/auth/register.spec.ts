import { test, expect } from '@playwright/test';
import { RegisterPage } from '../pages/register.page';

test.describe('Register Page', () => {
  let registerPage: RegisterPage;

  test.beforeEach(async ({ page }) => {
    registerPage = new RegisterPage(page);
    await registerPage.goto();
  });

  test('should display register heading and subtitle', async () => {
    await expect(registerPage.heading).toHaveText('Create Account');
    await expect(registerPage.subtitle).toHaveText('Join Yumney and start collecting recipes');
  });

  test('should display all form fields', async () => {
    await expect(registerPage.emailInput).toBeVisible();
    await expect(registerPage.displayNameInput).toBeVisible();
    await expect(registerPage.passwordInput).toBeVisible();
    await expect(registerPage.confirmPasswordInput).toBeVisible();
    await expect(registerPage.submitButton).toBeVisible();
  });

  test('should show required errors on empty form submission', async () => {
    await registerPage.submitButton.click();

    await expect(registerPage.fieldError('Email is required')).toBeVisible();
    await expect(registerPage.fieldError('Display name is required')).toBeVisible();
    await expect(registerPage.fieldError('Password is required')).toBeVisible();
    await expect(registerPage.fieldError('Please confirm your password')).toBeVisible();
  });

  test('should show invalid email error', async () => {
    await registerPage.emailInput.fill('not-an-email');
    await registerPage.emailInput.blur();

    await expect(registerPage.fieldError('Please enter a valid email address')).toBeVisible();
  });

  test('should show password minimum length error', async () => {
    await registerPage.passwordInput.fill('Abc1');
    await registerPage.passwordInput.blur();

    await expect(registerPage.fieldError('Password must be at least 8 characters')).toBeVisible();
  });

  test('should show password pattern error', async () => {
    await registerPage.passwordInput.fill('alllowercase');
    await registerPage.passwordInput.blur();

    await expect(registerPage.fieldError('Password must contain uppercase, lowercase, and a digit')).toBeVisible();
  });

  test('should show passwords mismatch error', async () => {
    await registerPage.passwordInput.fill('ValidPass1');
    await registerPage.confirmPasswordInput.fill('DifferentPass1');
    await registerPage.confirmPasswordInput.blur();

    await expect(registerPage.fieldError('Passwords do not match')).toBeVisible();
  });

  test('should show success message after successful registration', async ({ page }) => {
    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 200, json: { message: 'Registration successful' } }),
    );

    await registerPage.fillForm({
      email: 'test@example.com',
      displayName: 'Test User',
      password: 'ValidPass1',
      confirmPassword: 'ValidPass1',
    });
    await registerPage.submitButton.click();

    await expect(registerPage.successHeading).toBeVisible();
    await expect(registerPage.resendLink).toBeVisible();
  });

  test('should show error when email already exists', async ({ page }) => {
    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 409, json: { detail: 'Email already exists' } }),
    );

    await registerPage.fillForm({
      email: 'existing@example.com',
      displayName: 'Test User',
      password: 'ValidPass1',
      confirmPassword: 'ValidPass1',
    });
    await registerPage.submitButton.click();

    await expect(registerPage.errorBanner).toBeVisible();
    await expect(registerPage.errorBanner).toContainText('already exists');
  });

  test('should show generic error on server failure', async ({ page }) => {
    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 500, json: { detail: 'Internal error' } }),
    );

    await registerPage.fillForm({
      email: 'test@example.com',
      displayName: 'Test User',
      password: 'ValidPass1',
      confirmPassword: 'ValidPass1',
    });
    await registerPage.submitButton.click();

    await expect(registerPage.errorBanner).toBeVisible();
    await expect(registerPage.errorBanner).toContainText('unexpected error');
  });

  test('should navigate to resend verification after success', async ({ page }) => {
    await page.route('**/api/v1/auth/register', (route) =>
      route.fulfill({ status: 200, json: { message: 'OK' } }),
    );

    await registerPage.fillForm({
      email: 'test@example.com',
      displayName: 'Test User',
      password: 'ValidPass1',
      confirmPassword: 'ValidPass1',
    });
    await registerPage.submitButton.click();

    await registerPage.resendLink.click();
    await expect(page).toHaveURL(/\/auth\/resend-verification/);
  });

  test('should have correct page title', async ({ page }) => {
    await expect(page).toHaveTitle(/Register/);
  });
});
