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

test('public and authentication routes render cleanly in French and English', async ({ page }) => {
  const routes = [
    '/', '/services', '/services/bourses', '/services/comites', '/services/documents-officiels',
    '/actualites', '/actualites/evenements', '/actualites/annonces', '/actualites/souvenirs',
    '/engagement', '/engagement/annuaire', '/engagement/projets', '/engagement/consultations',
    '/contact', '/confidentialite', '/contribuer', '/espace-membre', '/admin/login',
  ];

  for (const language of ['fr', 'en']) {
    await page.goto('/');
    await page.getByRole('button', { name: language === 'fr' ? 'Français' : 'English' }).click();
    await expect(page.locator('html')).toHaveAttribute('lang', language);
    for (const route of routes) {
      await page.goto(route);
      await expect(page.locator('#root')).toBeVisible();
      await expect(page.locator('body')).not.toContainText(/\{\{\s*[\w.-]+(?:\s*,[^}]*)?\s*\}\}/);
      await expect(page.locator('body')).not.toContainText(/(?:public|admin)\.[a-z][\w.-]{2,}/);
      await expect(page.locator('html')).toHaveAttribute('lang', language);
    }
  }
});

test('privacy policy publishes account rights and the privacy contact', async ({ page }) => {
  await page.goto('/confidentialite');

  await expect(page.getByRole('heading', { name: /compte membre et finalités|member account and purposes/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /vos droits|your rights/i })).toBeVisible();
  await expect(page.getByRole('main')).toContainText(/image invisible|invisible image/i);
  await expect(page.getByRole('main')).toContainText(/désabonner en tout temps|unsubscribe at any time/i);
  await expect(page.getByRole('main').getByRole('link', { name: 'contact@hcbe.ca' })).toHaveAttribute('href', /^mailto:contact@hcbe\.ca/);
});

test('admin login page exposes an accessible sign-in form', async ({ page }) => {
  await page.goto('/admin/login');

  await expect(page.locator('input[name="email"]')).toBeVisible();
  await expect(page.locator('input[name="password"]')).toBeVisible();
  await expect(page.locator('button[type="submit"]')).toBeEnabled();
});

test('administrator can authenticate and reach the protected dashboard', async ({ page }) => {
  test.skip(!process.env.E2E_ADMIN_EMAIL || !process.env.E2E_ADMIN_PASSWORD, 'Admin E2E credentials are not configured');
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
  await expect(page.getByRole('heading', { name: /votre espace est prêt|your space is ready/i })).toBeVisible();
  await page.getByRole('tab', { name: /^associations$/i }).click();
  await expect(page.getByRole('heading', { name: /votre organisation, au même endroit|your organization, all in one place/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /rejoindre ou représenter une organisation|join or represent an organization/i })).toBeVisible();
  await page.getByRole('tab', { name: /occasions|opportunities/i }).click();
  await expect(page.getByRole('heading', { name: /mettez votre talent en mouvement|put your talent in motion/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /mes candidatures|my applications/i })).toBeVisible();
  await page.getByRole('tab', { name: /^notifications$/i }).click();
  await expect(page.getByRole('heading', { name: /mes notifications|my notifications/i })).toBeVisible();
  await page.getByRole('tab', { name: /mes préférences|my preferences/i }).click();
  await expect(page.getByText(/résumé communautaire|community digest/i)).toBeVisible();
  await expect(page.getByRole('heading', { name: /confidentialité et données personnelles|privacy and personal data/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /^télécharger$|^download$/i })).toBeEnabled();
  await expect(page.getByRole('button', { name: /demander la suppression|request deletion/i })).toBeEnabled();
});

test('visitor can prepare a contribution and reach the confirmed payment page', async ({ page }) => {
  await page.route('**/api/finance/campaigns', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data: [{
      id: '11111111-1111-1111-1111-111111111111', slug: 'entraide', title: "Fonds d'entraide",
      titleEn: 'Community support fund', description: 'Soutenir les initiatives communautaires.',
      descriptionEn: 'Support community initiatives.', goalAmountCents: 100000, raisedAmountCents: 25000,
      currency: 'cad', allowRecurring: true, isPublished: true, supporterCount: 8,
    }] }),
  }));
  await page.route('**/api/finance/donations/checkout', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data: { transactionId: '22222222-2222-2222-2222-222222222222', sessionId: 'cs_e2e', checkoutUrl: 'http://127.0.0.1:4173/paiement/merci?session_id=cs_e2e' } }),
  }));
  await page.route('**/api/finance/checkout/cs_e2e', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data: { status: 'Paid', kind: 'Donation', amountCents: 5000, currency: 'cad', receiptUrl: 'http://127.0.0.1:8080/api/finance/receipts/e2e' } }),
  }));

  await page.goto('/contribuer');
  await expect(page.getByRole('heading', { name: /chaque contribution|every contribution/i })).toBeVisible();
  await page.getByRole('textbox', { name: /^(courriel|email) \*$/i }).fill('donateur@hcbe.invalid');
  await page.getByRole('button', { name: /continuer vers le paiement|continue to secure payment/i }).click();

  await expect(page).toHaveURL(/paiement\/merci\?session_id=cs_e2e/);
  await expect(page.getByRole('heading', { name: /merci pour votre engagement|thank you for your commitment/i })).toBeVisible();
  const receiptLink = page.getByRole('link', { name: /télécharger le reçu pdf|download pdf receipt/i });
  await expect(receiptLink).toBeVisible();
  await expect(receiptLink).toHaveAttribute('download', '');
});

test('cancelled contribution checkout explains that no charge was made', async ({ page }) => {
  await page.route('**/api/finance/campaigns', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, data: [] }),
  }));
  await page.goto('/contribuer?payment=cancelled');
  await expect(page.getByRole('status')).toContainText(/aucun montant n’a été débité|nothing was charged/i);
});

test('event details offer Google, Outlook and Apple calendar actions', async ({ page }) => {
  const eventId = '33333333-3333-3333-3333-333333333333';
  await page.route(`**/api/events/${eventId}`, (route) => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, data: {
      id: eventId, title: 'Forum HCBE', titleEn: 'HCBE Forum', description: 'Rencontre communautaire',
      descriptionEn: 'Community gathering', date: '2026-11-12T18:00:00Z', endDate: '2026-11-12T20:00:00Z',
      timeZone: 'America/Toronto', location: 'Montréal', locationEn: 'Montreal', type: 'Conference', format: 'InPerson',
      status: 'Active', createdAt: '2026-09-01T00:00:00Z', updatedAt: '2026-09-01T00:00:00Z', speakers: [], organizers: [],
      registrationMode: 'External', registrationUrl: 'https://example.com/register', allowWaitlist: true,
      restrictMeetingLinkToRegistrants: false, confirmedRegistrationCount: 0, waitlistCount: 0, remainingCapacity: 100,
    } }),
  }));
  await page.goto(`/actualites/evenements/${eventId}`);
  await expect(page.getByRole('link', { name: /google/i })).toHaveAttribute('href', /calendar\.google\.com/);
  await expect(page.getByRole('link', { name: /outlook/i })).toHaveAttribute('href', /outlook\.live\.com/);
  await expect(page.getByRole('link', { name: /apple/i })).toHaveAttribute('href', /calendar\.ics/);
});
