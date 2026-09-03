# Production operations runbook

## Alert paths

- GitHub Actions runs the public uptime checks every ten minutes and opens one deduplicated issue on failure.
- The API writes JSON logs with correlation identifiers to Railway.
- Unhandled API exceptions are grouped in PostgreSQL and visible at `/admin/monitoring`.
- New and recurring incidents generate a maximum of one operations email per hour through the email outbox.
- Database backup or restore-verification failures mark the Railway cron deployment as crashed and trigger Railway deployment notifications.

For every production alert, record the trace identifier, affected route, first observed time, user impact, mitigation, root cause, and follow-up action. Resolve the in-app incident only after the correction is deployed and verified.

## Data Protection keys

The API stores its ASP.NET Data Protection key ring in the private Redis resource. Key XML is encrypted with AES-256-GCM before it enters Redis. `DataProtection__KeyEncryptionKeys` is a comma-separated list of Base64-encoded 32-byte keys, newest first.

To rotate safely:

1. Generate a new random 32-byte key and prepend it to the existing sealed value.
2. Deploy the API and verify logins, refresh tokens, and `/health/ready`.
3. Keep the prior key in the list for at least the lifetime of all Data Protection keys and protected payloads.
4. Remove the prior key during a later planned rotation, never during an incident unless compromise is confirmed.

Do not delete `hcbe:data-protection:key-ring` from Redis during routine deployment.

## Backup ownership and recovery objectives

- The Railway `postgres-backup` cron produces one independently verified logical dump each day with a 30-day retention window.
- Railway PITR is an optional additional rapid-recovery layer; it is currently disabled and must not be assumed in the recovery objective until it is explicitly enabled and restore-tested.
- Operations reviews the workflow every week and completes an isolated Railway restore drill quarterly.
- Initial targets: recovery point objective of 24 hours for the independent dump, and recovery time objective of four hours. Tighten these targets after Railway PITR is verified under a real drill.

The exact restore commands and validation checks are maintained in `hcbe-backend/docs/railway-deployment.md` and `ops/postgres-backup/backup.sh`.

The production drill completed successfully on 2026-09-03: PostgreSQL 18 restored the encrypted logical dump in isolation, validated 12 EF Core migrations plus the `Users`, `Members`, and `Events` tables, and uploaded the dump, SHA-256 checksum, and verification report to the private backup bucket.
