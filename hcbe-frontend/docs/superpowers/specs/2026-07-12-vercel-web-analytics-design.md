# Vercel Web Analytics — Design Spec

**Date:** 2026-07-12  
**Status:** Approved for implementation planning  
**Scope:** V1 — page views + traffic trends via Vercel Web Analytics (already enabled in Vercel)

## Goal

Give HCBE operators visibility into visitor traffic (top pages, views over time) without building a custom analytics backend, by wiring the official Vercel Analytics client into the React SPA.

## Decisions

| Topic | Choice |
|---|---|
| Product | External tool — Vercel Web Analytics |
| Dashboard | Vercel project dashboard (not an HCBE admin page) |
| Prerequisite | Web Analytics already enabled on `hcbe-frontend` |
| Client SDK | `@vercel/analytics` + `<Analytics />` at app root |
| Privacy | Short disclosure on the public privacy page (fr/en i18n) |
| Out of scope V1 | Custom events, Speed Insights, in-app admin charts, backend storage, cookie banner, Plausible/Umami/GA |

## Architecture

```
[Visitor] --> [React SPA on Vercel]
                    |
                    +-- <Analytics /> injects Vercel Web Analytics script
                    |
                    v
              Vercel Web Analytics (hosted)
                    |
                    v
         [Operator] Vercel Dashboard → Analytics
```

- No changes to `hcbe-backend` (.NET / SQLite).
- Mount `<Analytics />` once in `hcbe-frontend/src/App.tsx` (sibling of router), so all public and admin routes are covered. Admin traffic will appear; that is acceptable for V1 (filter by path in Vercel if needed).
- SPA route changes are handled by `@vercel/analytics` for React; verify after deploy that soft navigations create distinct page views.

## Implementation outline

1. Add dependency `@vercel/analytics` in `hcbe-frontend`.
2. Import and render `<Analytics />` in `App.tsx`.
3. Add privacy copy keys (`public.privacy.analyticsTitle` / `analyticsBody`) in `src/i18n/local/{fr,en}/newsletter.ts` (existing privacy strings live there) and a section in `src/pages/confidentialite/page.tsx`.
4. Document where to open stats: Vercel → project `hcbe-frontend` → Analytics (link in a short note under `docs/` or README snippet).

## Privacy / Loi 25 notes

- Prefer Vercel’s privacy-friendly Web Analytics mode (no advertising cookies).
- Privacy page must state that anonymous traffic metrics are collected via Vercel to improve the site.
- No separate cookie consent banner for V1 if the configured mode remains cookieless / non-advertising; revisit if Vercel settings change.

## Success criteria

- After production deploy, a page visit appears in Vercel Analytics within a reasonable delay.
- Top pages and time-series (day/week) are visible in the Vercel dashboard.
- Privacy page (fr + en) mentions analytics.
- No backend endpoints or DB tables added.

## Risks

| Risk | Mitigation |
|---|---|
| Free-tier volume limits | Fine for associative traffic; monitor in Vercel |
| SPA pageviews missing on client navigations | Smoke-test after deploy; consult Vercel React docs if needed |
| Admin routes inflate “top pages” | Accept for V1; path filters in dashboard |
| Spec vs reality of Vercel billing/UI | Confirm Analytics tab is active (already done by operator) |
