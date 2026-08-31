# Release and rollback runbook

## Environments

- Pull requests must pass backend and frontend CI.
- Pushes to `develop` deploy the isolated backend staging app after CI. Configure the GitHub `staging` environment variable `FLY_STAGING_APP_NAME` and its protected `FLY_API_TOKEN` secret.
- Pushes to `main` deploy production through the protected GitHub `production` environment.
- Staging and production require separate PostgreSQL databases, buckets, JWT secrets, SMTP credentials, and sender/test domains. Never point staging at production data or storage.

## Release

1. Confirm the database backup and restore drill are current.
2. Deploy to staging and verify `/health/live`, `/health/ready`, admin login/refresh/logout, one CMS create/edit/delete flow, one upload, and one outbox email.
3. Review migration SQL from the CI artifact. Destructive migrations require a compatible two-release expand/contract plan.
4. Approve the production environment deployment. The Fly release command applies migrations before application rollout.
5. Confirm `/health/ready`, error rate, request latency, outbox backlog, and representative public/admin flows.

## Rollback

1. Roll back application code to the previous known-good Fly release.
2. Do not automatically reverse a database migration. Prefer forward fixes; use `Down` only after verifying no newer data depends on the schema.
3. If data restoration is necessary, stop writes, create a safety backup, restore into a new database, validate it, and switch the connection secret atomically.
4. Retain object-storage versions and the legacy volume snapshot through the rollback window.

## Required alerts

Alert on readiness failures, elevated HTTP 5xx rate, sustained p95 latency, PostgreSQL connection exhaustion, low storage, dead-letter email, oldest pending outbox age over five minutes, and backup/restore verification failure.
