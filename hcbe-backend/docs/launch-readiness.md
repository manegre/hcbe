# Launch readiness record

This record captures the objective controls verified on 2026-09-06. It complements the release runbook; it does not replace the human acceptance check required for each release.

## Automated quality gates

- Backend release suite: 207 tests passed.
- Frontend TypeScript validation passed.
- Bilingual integrity: 1,855 French/English keys matched with compatible variables.
- Production frontend build and asset budgets passed.
- Browser suite: 18 scenarios covered public pages, authentication, membership, contributions, calendars, PWA installation, mobile/tablet administration, dark mode, and automated WCAG checks.
- Every public workspace now exposes exactly one `main#main-content` target for keyboard skip navigation.

## Production controls

- GitHub Actions validates pushes and pull requests.
- Railway **Wait for CI** is required on staging and production API/frontend services.
- Staging and production frontend, liveness, and readiness endpoints returned HTTP 200 before release.
- Production error incidents are captured in structured logs and the administration monitoring workspace.
- The scheduled uptime workflow checks frontend and API health every ten minutes and manages a GitHub incident issue.
- The 2026-09-06 PostgreSQL backup completed logical dump, isolated PostgreSQL 18 restore verification, encryption, checksum generation, upload, and retention cleanup.

## Release acceptance

After each deployment, follow `release-runbook.md`: verify the deployed revision, API migration, health endpoints, staging administrator and member flows, one CMS mutation, one upload, one outbox email, then production read-only smoke checks.

## Deliberately deferred

- Stripe live money movement remains disabled until business activation and an approved live payment/refund window.
- Apple Wallet and Google Wallet remain behind feature flags until issuer credentials are approved.
- WhatsApp delivery remains disabled until the organization completes its provider and consent setup.
