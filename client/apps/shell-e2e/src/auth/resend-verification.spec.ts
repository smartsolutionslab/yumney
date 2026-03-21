import { test, expect } from '@playwright/test';
import { ResendVerificationPage } from '../pages/resend-verification.page';

test.describe('Resend Verification Page', () => {
  let resendPage: ResendVerificationPage;

  test.beforeEach(async ({ page }) => {
    resendPage = new ResendVerificationPage(page);
    await resendPage.goto();
  });

  test('should display heading and subtitle', async () => {
    await expect(resendPage.heading).toHaveText('Resend Verification Email');
    await expect(resendPage.subtitle).toContainText('Enter your email address');
  });

  test('should display email input and submit button', async () => {
    await expect(resendPage.emailInput).toBeVisible();
    await expect(resendPage.submitButton).toBeVisible();
  });

  test('should show required error on empty submission', async () => {
    await resendPage.submitButton.click();

    await expect(resendPage.fieldError('Email is required')).toBeVisible();
  });

  test('should show invalid email error', async () => {
    await resendPage.emailInput.fill('not-valid');
    await resendPage.emailInput.blur();

    await expect(resendPage.fieldError('Please enter a valid email address')).toBeVisible();
  });

  test('should show success message after successful resend', async ({ page }) => {
    await page.route('**/api/v1/auth/resend-verification-email', (route) =>
      route.fulfill({ status: 200, json: { message: 'Email sent' } }),
    );

    await resendPage.emailInput.fill('test@example.com');
    await resendPage.submitButton.click();

    await expect(resendPage.successHeading).toBeVisible();
    await expect(resendPage.successMessage.first()).toContainText('verification email has been sent');
  });

  test('should show error on service unavailability', async ({ page }) => {
    await page.route('**/api/v1/auth/resend-verification-email', (route) =>
      route.fulfill({ status: 503, json: { detail: 'Service unavailable' } }),
    );

    await resendPage.emailInput.fill('test@example.com');
    await resendPage.submitButton.click();

    await expect(resendPage.errorBanner).toBeVisible();
  });

  test('should have back to registration link', async ({ page }) => {
    await expect(resendPage.backToRegisterLink).toBeVisible();
    await resendPage.backToRegisterLink.click();
    await expect(page).toHaveURL(/\/auth\/register/);
  });

  test('should have correct page title', async ({ page }) => {
    await expect(page).toHaveTitle(/Resend Verification/);
  });
});
