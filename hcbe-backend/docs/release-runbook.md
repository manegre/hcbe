# Release and rollback runbook

## Environments

- GitHub Actions validates every pull request and every push to `main`.
- Railway hosts isolated `staging` and `production` environments. Each has its own API, frontend, PostgreSQL database, Redis instance, object-storage buckets, secrets, and domains.
- The four application services follow `main` and have **Wait for CI** enabled. Railway must not deploy a revision until the GitHub checks for that revision succeed.
- Stripe live payments, Apple/Google Wallet, and WhatsApp remain disabled until their separate launch approvals are complete.

## Release

1. Confirm the latest encrypted PostgreSQL backup completed its isolated restore verification.
2. Run `npm run restore:backend`, `npm run build:backend`, `npm run verify`, and `npm run test:e2e` locally when the change can affect critical flows.
3. Review migration SQL. Destructive migrations require a compatible two-release expand/contract plan.
4. Push the reviewed commit to `main` and wait for the complete **Monorepo CI** workflow. Railway deploys only after it succeeds.
5. Confirm the staging and production API pre-deploy migration commands succeed, both services become healthy, and `/health/live` plus `/health/ready` return HTTP 200.
6. On staging, verify admin login/refresh/logout, one CMS create/edit/delete flow, one upload, one outbox email, member login, and the affected business workflow.
7. On production, perform read-only smoke checks of the public site and administration login page. Exercise money movement only during an approved payment test window.
8. Review application incidents, request latency, email outbox health, and the production uptime workflow before closing the release.

Staging and production currently follow the same protected branch and therefore begin deployment after the same successful CI run. If HCBE later requires a manual promotion gate, connect staging to a dedicated release-candidate branch and keep production on `main`.

## Rollback

1. In Railway, redeploy the previous known-good deployment for the affected service.
2. Do not automatically reverse a database migration. Prefer a forward fix; use an EF Core `Down` migration only after verifying that no newer data depends on the schema.
3. If data restoration is necessary, stop writes, create a safety backup, restore into a new isolated database, validate it, and switch the connection reference atomically.
4. Retain object-storage versions and the prior deployment through the agreed rollback window.
5. Re-run health checks and the representative read-only flows after rollback.

## Required alerts

Alert on readiness failures, elevated HTTP 5xx rate, sustained p95 latency, PostgreSQL connection exhaustion, low storage, dead-letter email, oldest pending outbox age over five minutes, and backup/restore verification failure. The operations team must subscribe to GitHub outage issues and monitor **Administration → Surveillance**.
