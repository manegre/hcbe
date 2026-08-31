import { expect, test } from '@playwright/test';

test('public home page renders the application shell', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveTitle(/HCBE Canada/i);
  await expect(page.locator('#root')).toBeVisible();
  await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
});

test('admin login page exposes an accessible sign-in form', async ({ page }) => {
  await page.goto('/admin/login');

  await expect(page.locator('input[name="email"]')).toBeVisible();
  await expect(page.locator('input[name="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeEnabled();
});
