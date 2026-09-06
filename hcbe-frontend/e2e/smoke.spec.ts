import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('public home page renders the application shell', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveTitle(/HCBE Canada/i);
  await expect(page.locator('#root')).toBeVisible();
  await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
});

test('PWA manifest, offline fallback and service worker are production-ready', async ({ page, request }) => {
  const manifestResponse = await request.get('/manifest.webmanifest');
  expect(manifestResponse.ok()).toBeTruthy();
  expect(manifestResponse.headers()['content-type']).toMatch(/application\/(?:manifest\+json|json)/i);
  const manifest = await manifestResponse.json();
  expect(manifest.display).toBe('standalone');
  expect(manifest.icons).toEqual(expect.arrayContaining([
    expect.objectContaining({ sizes: '192x192', type: 'image/png' }),
    expect.objectContaining({ sizes: '512x512', type: 'image/png' }),
    expect.objectContaining({ sizes: '512x512', type: 'image/png', purpose: 'maskable' }),
  ]));
  expect(manifest.shortcuts.length).toBeGreaterThanOrEqual(3);
  expect((await request.get('/offline.html')).ok()).toBeTruthy();
  expect(await (await request.get('/sw.js')).text()).toContain("addEventListener('push'");
  await page.goto('/');
  await expect.poll(() => page.evaluate(() => navigator.serviceWorker.getRegistration().then(Boolean))).toBeTruthy();
});

test('iPhone and Android visitors can open platform-specific installation guidance', async ({ browser }) => {
  const devices = [
    {
      name: 'iPhone · iPad',
      language: 'fr',
      userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1',
      install: /installer l.application/i,
      admin: /connexion admin/i,
      heading: /installez l.application/i,
      instruction: /sur l’écran d’accueil/i,
    },
    {
      name: 'Android',
      language: 'en',
      userAgent: 'Mozilla/5.0 (Linux; Android 15; Pixel 9) AppleWebKit/537.36 Chrome/139.0 Mobile Safari/537.36',
      install: /install the app/i,
      admin: /admin sign-in/i,
      heading: /install the app/i,
      instruction: /add to home screen/i,
    },
  ];

  for (const device of devices) {
    const context = await browser.newContext({ userAgent: device.userAgent, viewport: { width: 390, height: 844 } });
    await context.addInitScript((language) => localStorage.setItem('i18nextLng', language), device.language);
    const page = await context.newPage();
    await page.goto('/');
    await page.getByRole('button', { name: /ouvrir le menu|open menu/i }).click();
    const adminAccess = page.getByRole('link', { name: device.admin });
    await expect(adminAccess).toBeVisible();
    await expect(adminAccess).toHaveAttribute('href', '/admin/login');
    await page.getByRole('button', { name: device.install }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: device.heading })).toBeVisible();
    await expect(dialog).toContainText(device.name);
    await expect(dialog).toContainText(device.instruction);
    await context.close();
  }
});

test('installed mobile app keeps the admin sign-in accessible', async ({ browser }) => {
  const context = await browser.newContext({
    userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 Version/18.0 Mobile/15E148 Safari/604.1',
    viewport: { width: 390, height: 844 },
  });
  await context.addInitScript(() => {
    localStorage.setItem('i18nextLng', 'fr');
    Object.defineProperty(navigator, 'standalone', { configurable: true, get: () => true });
  });
  const page = await context.newPage();
  await page.goto('/');
  await page.getByRole('button', { name: /ouvrir le menu/i }).click();
  await expect(page.getByRole('link', { name: /connexion admin/i })).toHaveAttribute('href', '/admin/login');
  await expect(page.getByRole('button', { name: /installer l.application/i })).toHaveCount(0);
  await context.close();
});

test('representative public routes meet automated WCAG 2.2 AA checks', async ({ page }) => {
  test.setTimeout(90_000);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  const violations: string[] = [];
  for (const route of ['/', '/services', '/communaute/ressources', '/actualites/evenements', '/contact', '/espace-membre', '/admin/login']) {
    await page.goto(route);
    await page.locator('main').first().waitFor();
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .exclude('[aria-hidden="true"]')
      .analyze();
    for (const violation of results.violations) {
      for (const node of violation.nodes) {
        violations.push(`${route}: ${violation.id}: ${node.target.join(' > ')} — ${node.failureSummary?.replace(/\s+/g, ' ')}`);
      }
    }
  }
  expect(violations).toEqual([]);
});

test('keyboard users can skip repeated navigation', async ({ page }) => {
  await page.goto('/');
  await page.locator('main#main-content').waitFor();
  await page.keyboard.press('Tab');
  const skip = page.getByRole('link', { name: /aller au contenu principal|skip to main content/i });
  await expect(skip).toBeFocused();
  await skip.press('Enter');
  await expect(page.locator('main#main-content')).toBeFocused();
});

test('every public workspace exposes a single skip-link target', async ({ page }) => {
  const routes = [
    '/', '/services', '/services/bourses', '/services/comites', '/services/documents-officiels',
    '/actualites', '/actualites/evenements', '/actualites/annonces', '/actualites/souvenirs',
    '/engagement', '/engagement/annuaire', '/engagement/projets', '/engagement/consultations',
    '/contact', '/confidentialite', '/contribuer', '/communaute/ressources', '/espace-membre',
  ];

  for (const route of routes) {
    await page.goto(route);
    await expect(page.locator('main#main-content')).toHaveCount(1);
    await expect(page.getByRole('link', { name: /aller au contenu principal|skip to main content/i })).toHaveAttribute('href', '#main-content');
  }
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
  test.setTimeout(90_000);
  const routes = [
    '/', '/services', '/services/bourses', '/services/comites', '/services/documents-officiels',
    '/actualites', '/actualites/evenements', '/actualites/annonces', '/actualites/souvenirs',
    '/engagement', '/engagement/annuaire', '/engagement/projets', '/engagement/consultations',
    '/contact', '/confidentialite', '/contribuer', '/communaute/ressources', '/espace-membre', '/admin/login',
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
  test.setTimeout(90_000);
  test.skip(!process.env.E2E_ADMIN_EMAIL || !process.env.E2E_ADMIN_PASSWORD, 'Admin E2E credentials are not configured');
  await page.goto('/admin/login', { waitUntil: 'domcontentloaded' });
  await page.locator('input[name="email"]').fill(process.env.E2E_ADMIN_EMAIL!);
  await page.locator('input[name="password"]').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.locator('button[type="submit"]').click();

  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.locator('main')).toBeVisible();
  await page.goto('/admin/impact');
  await expect(page.getByRole('heading', { name: /du compte à la première participation|from account to first participation/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /rapport pdf|pdf report/i })).toBeEnabled();
  await page.locator('#impact-period').selectOption('12');
  await expect(page.locator('#impact-period')).toHaveValue('12');
  await expect(page.getByRole('img', { name: /12 mois|12 months/i })).toBeVisible();

  await page.goto('/admin/members');
  await expect(page.getByRole('heading', { name: /registre fiable et portable|reliable, portable registry/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /exporter csv|export csv/i })).toBeEnabled();
  await expect(page.getByRole('button', { name: /importer csv|import csv/i })).toBeEnabled();
  await page.getByRole('button', { name: /chercher les doublons|find duplicates/i }).click();
  await expect(page.getByText(/aucun doublon probable détecté|no likely duplicates detected|confiance|confidence/i).first()).toBeVisible();

  await page.goto('/admin/newsletter');
  await expect(page.getByRole('heading', { level: 1, name: /^infolettre$|^newsletter$/i })).toBeVisible();
  await expect(page.getByRole('status')).toContainText(/canaux sont opérationnels|channels are operational|requiert votre attention|needs attention/i);
  await expect(page.getByRole('button', { name: /courriel|email/i }).first()).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: /dans l.application|in the app/i }).click();
  await page.getByRole('button', { name: /notification poussée|push notification/i }).click();
  await expect(page.locator('#campaign-body')).toBeVisible();
  expect((await page.locator('#campaign-body').boundingBox())?.height).toBeGreaterThanOrEqual(200);
  await page.getByRole('button', { name: /calculer|calculate/i }).click();
  await expect(page.getByText(/^destinataires$|^recipients$/i)).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  if (process.env.E2E_CAPTURE_VISUALS) {
    await page.screenshot({ path: 'test-results/admin-newsletter-mobile.png', fullPage: true });
  }
  await page.setViewportSize({ width: 1280, height: 900 });

  await page.goto('/admin/security');
  await expect(page.getByRole('heading', { level: 1, name: /centre de sécurité|security centre/i })).toBeVisible();
  await page.getByRole('button', { name: /signaler un incident|report an incident/i }).click();
  const editor = page.locator('#incident-description');
  await expect(editor).toBeVisible();
  expect((await editor.boundingBox())?.height).toBeGreaterThanOrEqual(260);
  await expect(page.getByRole('toolbar', { name: /mise en forme du texte|text formatting/i }).first()).toBeVisible();
  await editor.fill('**Résumé professionnel**\n\n- Première mesure\n- Deuxième mesure');
  await page.getByRole('button', { name: /aperçu|preview/i }).first().click();
  await expect(page.locator('strong').filter({ hasText: /résumé professionnel/i })).toBeVisible();
  if (process.env.E2E_CAPTURE_VISUALS) {
    await page.screenshot({ path: 'test-results/admin-security-desktop.png', fullPage: true });
  }

  await page.setViewportSize({ width: 390, height: 844 });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  await page.evaluate(() => localStorage.setItem('hcbe-theme', 'dark'));
  await page.reload();
  await page.getByRole('button', { name: /signaler un incident|report an incident/i }).click();
  await expect(page.locator('html')).toHaveClass(/dark/);
  await expect.poll(() => page.locator('.rich-text-editor').first().evaluate((node) => getComputedStyle(node).backgroundColor)).not.toBe('rgb(255, 255, 255)');
  if (process.env.E2E_CAPTURE_VISUALS) {
    await page.screenshot({ path: 'test-results/admin-security-mobile-dark.png', fullPage: true });
  }

  await page.setViewportSize({ width: 1280, height: 900 });
  for (const route of [
    '/admin/events/create', '/admin/news/create', '/admin/projects/create',
    '/admin/documents/create', '/admin/associations/create', '/admin/grants/create',
    '/admin/consultations/create', '/admin/team-members/create',
  ]) {
    await page.goto(route);
    await expect(page.locator('.rich-text-editor').first()).toBeVisible();
    await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  }
});

test('authenticated administration routes meet automated WCAG 2.2 AA checks', async ({ page }) => {
  test.setTimeout(120_000);
  test.skip(!process.env.E2E_ADMIN_EMAIL || !process.env.E2E_ADMIN_PASSWORD, 'Admin E2E credentials are not configured');
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/admin/login', { waitUntil: 'domcontentloaded' });
  await page.locator('input[name="email"]').fill(process.env.E2E_ADMIN_EMAIL!);
  await page.locator('input[name="password"]').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);

  const violations: string[] = [];
  for (const route of ['/admin/dashboard', '/admin/impact', '/admin/members', '/admin/security']) {
    await page.goto(route, { waitUntil: 'domcontentloaded' });
    await page.locator('main').first().waitFor();
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
      .exclude('[aria-hidden="true"]')
      .analyze();
    for (const violation of results.violations) {
      for (const node of violation.nodes) violations.push(`${route}: ${violation.id}: ${node.target.join(' > ')} — ${node.failureSummary?.replace(/\s+/g, ' ')}`);
    }
  }
  expect(violations).toEqual([]);
});

test('every administrator workspace remains usable on mobile and tablet in dark mode', async ({ page }) => {
  test.setTimeout(240_000);
  test.skip(!process.env.E2E_ADMIN_EMAIL || !process.env.E2E_ADMIN_PASSWORD, 'Admin E2E credentials are not configured');
  await page.goto('/admin/login', { waitUntil: 'domcontentloaded' });
  await page.locator('input[name="email"]').fill(process.env.E2E_ADMIN_EMAIL!);
  await page.locator('input[name="password"]').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await page.evaluate(() => localStorage.setItem('hcbe-theme', 'dark'));

  const routes = [
    '/admin/dashboard', '/admin/events', '/admin/news', '/admin/documents', '/admin/associations',
    '/admin/association-requests', '/admin/projects', '/admin/opportunities', '/admin/grants',
    '/admin/consultations', '/admin/members', '/admin/membership-applications', '/admin/newsletter',
    '/admin/mentorship', '/admin/message-reports', '/admin/submissions', '/admin/service-cases',
    '/admin/impact', '/admin/monitoring', '/admin/security', '/admin/finance', '/admin/users',
    '/admin/marketplace', '/admin/community-programs', '/admin/partners', '/admin/site-content', '/admin/team-members',
  ];

  await page.setViewportSize({ width: 390, height: 844 });
  for (const route of routes) {
    await page.goto(route, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('main').first()).toBeVisible();
    await expect(page.locator('html')).toHaveClass(/dark/);
    await expect(page.locator('body')).not.toHaveText(/unexpected application error/i);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
    const table = page.locator('.admin-data-table').first();
    if (await table.count()) await expect.poll(() => table.evaluate((element) => element.scrollWidth <= element.clientWidth)).toBeTruthy();
    if (process.env.E2E_CAPTURE_VISUALS && route === '/admin/grants') {
      await page.screenshot({ path: 'test-results/admin-table-mobile-dark.png', fullPage: true });
    }
  }

  await page.setViewportSize({ width: 768, height: 1024 });
  for (const route of ['/admin/dashboard', '/admin/events', '/admin/newsletter', '/admin/security', '/admin/site-content']) {
    await page.goto(route, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('main').first()).toBeVisible();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
  }
});

test('member can register and enter the member portal', async ({ page }) => {
  const memberEmail = `awa.e2e.${Date.now()}@hcbe.invalid`;
  await page.setViewportSize({ width: 390, height: 844 });
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
  await expect(page.getByRole('heading', { name: /choisi selon votre profil|selected for your profile/i })).toBeVisible();
  await page.getByRole('tab', { name: /mon adhésion|my membership/i }).click();
  await expect(page.getByRole('heading', { name: /membre communautaire — gratuit|community member — free/i })).toBeVisible();
  await expect(page.getByRole('main')).toContainText(/aucun paiement n’est requis|no payment is required/i);
  await expect(page.getByRole('button', { name: /déjà renouvelée|already renewed/i })).toBeDisabled();
  await expect(page.getByRole('button', { name: /télécharger en pdf|download pdf/i })).toBeEnabled();
  await expect(page.getByRole('button', { name: /imprimer la carte|print card/i })).toBeEnabled();
  await expect(page.getByText('Apple Wallet')).toHaveCount(0);
  await expect(page.getByText('Google Wallet')).toHaveCount(0);
  await page.getByRole('tab', { name: /^associations$/i }).click();
  await expect(page.getByRole('heading', { name: /votre organisation, au même endroit|your organization, all in one place/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /rejoindre ou représenter une organisation|join or represent an organization/i })).toBeVisible();
  await page.getByRole('tab', { name: /occasions|opportunities/i }).click();
  await expect(page.getByRole('heading', { name: /mettez votre talent en mouvement|put your talent in motion/i })).toBeVisible();
  await expect(page.getByRole('heading', { name: /mes candidatures|my applications/i })).toBeVisible();
  await page.getByRole('tab', { name: /organisateur|organizer/i }).click();
  await expect(page.getByRole('heading', { name: /espace organisateur|organizer workspace/i })).toBeVisible();
  await expect(page.locator('#org-name')).toBeVisible();
  await page.getByRole('tab', { name: /services\+/i }).click();
  await expect(page.getByRole('heading', { name: /premières étapes au Canada|first steps in Canada/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /famille|family/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /rendez-vous|appointments/i })).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBeTruthy();
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
