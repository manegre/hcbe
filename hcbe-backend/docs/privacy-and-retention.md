# Privacy and retention

Authenticated members can use the privacy centre under **Espace membre → Mes préférences** to download a structured JSON copy of their account data from `GET /api/privacy/export`. The export includes account/profile data, preferences, user-submitted requests, registrations, community activity, and public forms associated with the account email. Secrets, password hashes, reset tokens, refresh tokens, internal delivery records, and unrelated administrative records are excluded.

`POST /api/privacy/deletion-request` starts a 30-day grace period. Optional communications, newsletter subscriptions, directory visibility, connection requests, and mentorship sharing consent are withdrawn immediately. `DELETE /api/privacy/deletion-request` cancels while pending, but does not silently restore those choices. Administrator accounts cannot self-delete until their responsibilities are transferred. When due, the retention worker invalidates sessions, disables the account, removes user-uploaded service-case files, and anonymizes PII across member, application, public-submission, directory, mentorship, connection, message, newsletter, email-delivery, and audit records while preserving referential integrity and moderation history. The irreversible operation retains only a one-way subject reference for compliance tracking.

Member campaign preferences are opt-in by default. A preference record that has not been explicitly completed does not authorize optional member campaigns. Turning off the newsletter from the member preference centre also deactivates a matching public newsletter subscription. Essential security and service messages are not treated as optional campaigns.

Privacy endpoints use authenticated-user rate limits: three exports per hour and two deletion/cancellation writes per hour. Privacy-request administration is restricted to administrators with member-management permission.

Default operational retention:

- expired password reset tokens: 7 days;
- expired or revoked refresh tokens: 30 days;
- successful email outbox records: 90 days;
- dead-letter email records: 365 days;
- notifications: 365 days;
- audit logs: 730 days (configurable with `Privacy__AuditRetentionDays`).

The public policy identifies the HCBE Canada privacy officer by title at `contact@hcbe.ca` and states the normal 30-day response deadline for verified written requests. The organization must still assign this role to a named person internally, confirm the retention defaults with Québec privacy counsel, document lawful exceptions/holds, maintain privacy impact assessments and a confidentiality-incident register, execute data-processing agreements, and rehearse the written-request and incident-response procedures.
