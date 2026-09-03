import { expect, test } from '@playwright/test';

test('public home page renders the application shell', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveTitle(/HCBE Canada/i);
  await expect(page.locator('#root')).toBeVisible();
  await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
});

test('public services and events pages load against the real API', async ({ page, request }) => {
  const apiUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:8080';
  const readiness = await request.get(`${apiUrl}/health/ready`);
  expect(readiness.ok()).toBeTruthy();

  for (const path of ['/services', '/actualites/evenements']) {
    await page.goto(path);
    await expect(page.locator('#root')).toBeVisible();
    await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
  }
});

test('privacy policy publishes account rights and the privacy contact', async ({ page }) => {
  await page.goto('/confidentialite');

  await expect(page.getByRole('heading', { name: /compte membre et finalités|member account and purposes/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /vos droits|your rights/i })).toBeVisible();
  await expect(page.getByRole('link', { name: 'contact@hcbe.ca' })).toHaveAttribute('href', /^mailto:contact@hcbe\.ca/);
});

test('admin login page exposes an accessible sign-in form', async ({ page }) => {
  await page.goto('/admin/login');

  await expect(page.locator('input[name="email"]')).toBeVisible();
  await expect(page.locator('input[name="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeEnabled();
});

test('administrator can authenticate and reach the protected dashboard', async ({ page }) => {
  await page.goto('/admin/login');
  await page.locator('input[name="email"]').fill(process.env.E2E_ADMIN_EMAIL!);
  await page.locator('input[name="password"]').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.locator('button[type="submit"]').click();

  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.locator('main')).toBeVisible();
});

test('member can register and enter the member portal', async ({ page }) => {
  const memberEmail = `awa.e2e.${Date.now()}@hcbe.invalid`;
  await page.goto('/espace-membre');
  await page.getByRole('tab', { name: /créer un compte|create account|sign up/i }).click();
  await page.locator('#prenom').fill('Awa');
  await page.locator('#nom').fill('E2E');
  await page.locator('#member-email').fill(memberEmail);
  await page.locator('#telephone').fill('+1 514 555 0101');
  await page.locator('#ville').fill('Montréal');
  await page.locator('#province').selectOption({ index: 1 });
  await page.locator('#member-password').fill('HCBE-Member-2026!');
  await page.locator('#member-confirm-password').fill('HCBE-Member-2026!');
  await page.locator('#motivationAdhesion').fill('Participer aux activités de la communauté HCBE.');
  await page.locator('#inscription-membre-form button[type="submit"]').click();

  await expect(page.getByRole('navigation', { name: /communauté des membres|member community/i })).toBeVisible();
  await page.getByRole('tab', { name: /mes préférences|my preferences/i }).click();
  await expect(page.getByRole('heading', { name: /confidentialité et données personnelles|privacy and personal data/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /^télécharger$|^download$/i })).toBeEnabled();
  await expect(page.getByRole('button', { name: /demander la suppression|request deletion/i })).toBeEnabled();
});
