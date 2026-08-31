# Privacy and retention

Authenticated members can download a JSON copy of their account data from `GET /api/privacy/export`. Secrets, password hashes, reset tokens, refresh tokens, internal delivery records, and unrelated administrative records are excluded.

`POST /api/privacy/deletion-request` starts a 30-day grace period. `DELETE /api/privacy/deletion-request` cancels while pending. Administrator accounts cannot self-delete until their responsibilities are transferred. When due, the retention worker invalidates sessions, disables the account, and anonymizes PII across member, application, directory, mentorship, connection, message, newsletter, email-delivery, and audit records while preserving referential integrity and moderation history. The irreversible operation retains only a one-way subject reference for compliance tracking.

Default operational retention:

- expired password reset tokens: 7 days;
- expired or revoked refresh tokens: 30 days;
- successful email outbox records: 90 days;
- dead-letter email records: 365 days;
- notifications: 365 days;
- audit logs: 730 days (configurable with `Privacy__AuditRetentionDays`).

The organization must confirm these defaults with Canadian privacy counsel, publish the final retention policy, document lawful exceptions/holds, designate a privacy owner, and establish a process for identity verification and statutory response deadlines.
