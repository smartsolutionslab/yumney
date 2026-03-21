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

  test('should submit resend request and show success', async () => {
    await resendPage.emailInput.fill('test@yumney.dev');
    await resendPage.submitButton.click();

    await expect(resendPage.successHeading).toBeVisible({ timeout: 10_000 });
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
