# Newsletter V1 Implementation Plan

> **For agentic workers:** Execute task-by-task. Steps use checkbox syntax.

**Goal:** Public newsletter signup (home + footer) with CASL consent, privacy page, and admin list/filter/deactivate/CSV export.

**Architecture:** Backend `NewsletterSubscription` + anonymous `POST /api/newsletter/subscribe` + admin endpoints; frontend `NewsletterSignup` variants; remove dead Readdy `NewsletterSection`.

**Tech Stack:** ASP.NET minimal APIs, EF Core, React + i18n, existing admin auth.

**Spec:** `docs/superpowers/specs/2026-07-12-newsletter-design.md`

---

### Task 1: Backend model, DTOs, DbContext, ensure-table, service, endpoints

- [ ] Model `NewsletterSubscription`
- [ ] DTOs + service + endpoints
- [ ] Wire Program.cs + ApplicationDbContext
- [ ] Service unit tests

### Task 2: Frontend API + NewsletterSignup + home/footer + privacy page

- [ ] `newsletter.ts` API client + types
- [ ] `NewsletterSignup` component (home + footer variants)
- [ ] Home section, Footer integration
- [ ] `/confidentialite` page + route
- [ ] i18n FR/EN
- [ ] Delete unused `NewsletterSection.tsx`

### Task 3: Admin newsletter page

- [ ] `/admin/newsletter` list, filters, deactivate, export CSV
- [ ] Router + Layout nav + i18n

### Task 4: Verify

- [ ] Backend tests pass
- [ ] Frontend builds / no unused exports
