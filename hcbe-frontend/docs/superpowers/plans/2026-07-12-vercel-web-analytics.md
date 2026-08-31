# Vercel Web Analytics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire `@vercel/analytics` into the React SPA so page views appear in the already-enabled Vercel Web Analytics dashboard, and disclose analytics on the privacy page.

**Architecture:** Mount `<Analytics />` once in `App.tsx`. No backend changes. Privacy copy via existing i18n keys. Short operator note under `docs/`.

**Tech Stack:** Vite + React 19, `@vercel/analytics/react`, react-i18next

**Spec:** `docs/superpowers/specs/2026-07-12-vercel-web-analytics-design.md`

---

## File map

| File | Responsibility |
|---|---|
| `hcbe-frontend/package.json` | Add `@vercel/analytics` dependency |
| `hcbe-frontend/src/App.tsx` | Render `<Analytics />` at app root |
| `hcbe-frontend/src/i18n/local/fr/newsletter.ts` | FR privacy analytics strings |
| `hcbe-frontend/src/i18n/local/en/newsletter.ts` | EN privacy analytics strings |
| `hcbe-frontend/src/pages/confidentialite/page.tsx` | Render analytics section |
| `docs/vercel-web-analytics.md` | Where operators open the dashboard |

---

### Task 1: Install `@vercel/analytics`

**Files:**
- Modify: `hcbe-frontend/package.json`

- [ ] **Step 1: Install package**

```bash
cd hcbe-frontend && npm install @vercel/analytics
```

Expected: `@vercel/analytics` listed under `dependencies`.

- [ ] **Step 2: Commit** (only if user requests)

```bash
git add package.json package-lock.json
git commit -m "$(cat <<'EOF'
Add @vercel/analytics dependency for Web Analytics.

EOF
)"
```

---

### Task 2: Mount Analytics in App

**Files:**
- Modify: `hcbe-frontend/src/App.tsx`

- [ ] **Step 1: Add import and component**

Use React import (not Next.js):

```tsx
import { Analytics } from '@vercel/analytics/react';
```

Place `<Analytics />` inside the existing tree, e.g. after `BackToTopButton` and still inside `BrowserRouter` (or as sibling inside `AuthProvider` — either works; prefer inside the outermost return so it mounts once):

```tsx
function App() {
  return (
    <I18nextProvider i18n={i18n}>
      <DocumentLanguageSync />
      <AuthProvider>
        <BrowserRouter basename={__BASE_PATH__}>
          <ScrollToTop />
          <AppRoutes />
          <BackToTopButton />
        </BrowserRouter>
        <Analytics />
      </AuthProvider>
    </I18nextProvider>
  );
}
```

- [ ] **Step 2: Typecheck / build**

```bash
cd hcbe-frontend && npm run build
```

Expected: build succeeds.

---

### Task 3: Privacy disclosure (fr/en)

**Files:**
- Modify: `hcbe-frontend/src/i18n/local/fr/newsletter.ts`
- Modify: `hcbe-frontend/src/i18n/local/en/newsletter.ts`
- Modify: `hcbe-frontend/src/pages/confidentialite/page.tsx`

- [ ] **Step 1: Add FR keys** (after newsletter privacy body, before retention):

```ts
  'public.privacy.analyticsTitle': 'Statistiques de fréquentation',
  'public.privacy.analyticsBody':
    'Nous utilisons Vercel Web Analytics pour mesurer anonymement la fréquentation du site (pages consultées, tendances dans le temps). Ces mesures ne servent pas à la publicité et n’utilisent pas de cookies publicitaires.',
```

- [ ] **Step 2: Add EN keys**

```ts
  'public.privacy.analyticsTitle': 'Traffic analytics',
  'public.privacy.analyticsBody':
    'We use Vercel Web Analytics to measure anonymous site traffic (pages viewed, trends over time). These metrics are not used for advertising and do not rely on advertising cookies.',
```

- [ ] **Step 3: Render section on privacy page** (after newsletter block):

```tsx
          <div>
            <h2 className="text-2xl font-bold text-gray-950">{t('public.privacy.analyticsTitle')}</h2>
            <p className="mt-3 text-base leading-7 text-gray-600">{t('public.privacy.analyticsBody')}</p>
          </div>
```

---

### Task 4: Operator doc

**Files:**
- Create: `docs/vercel-web-analytics.md`

- [ ] **Step 1: Write short guide**

Content must include:
- Web Analytics already enabled on project `hcbe-frontend`
- Dashboard: https://vercel.com → project → **Analytics**
- After deploy with `<Analytics />`, page views appear (may take a few minutes)
- Top pages and time ranges (day/week) are available in that UI
- No HCBE admin page for traffic in V1

- [ ] **Step 2: Link from `docs/README.md`** under a short “Produit / ops” bullet

---

### Task 5: Verify

- [ ] **Step 1:** `npm run build` in `hcbe-frontend` passes
- [ ] **Step 2:** Confirm no backend files changed
- [ ] **Step 3:** After production deploy, open a public page and check Vercel Analytics for a new view

---

## Spec coverage

| Spec item | Task |
|---|---|
| `@vercel/analytics` + `<Analytics />` | 1–2 |
| Privacy page fr/en | 3 |
| Operator doc / where to read stats | 4 |
| No backend | verified in 5 |
| Out of scope (events, Speed Insights, admin UI) | not implemented |
