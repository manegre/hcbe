# Reliable email delivery

Password-reset and newsletter email is committed to `EmailOutboxMessages` in the same database transaction as the related application state. `EmailOutboxWorker` processes messages in small batches, recovers stale processing locks, and retries failures with exponential backoff. After five failed attempts a message moves to `DeadLetter`.

Administrators can inspect `GET /api/admin/email-outbox` and explicitly retry an unsent message with `POST /api/admin/email-outbox/{id}/retry`. HTML bodies are intentionally omitted from the list response.

Railway Free, Trial, and Hobby plans block outbound SMTP. Production therefore uses Brevo's
transactional HTTPS API with these secrets/settings:

- `Email__Mode=BrevoApi`
- `Email__Brevo__ApiKey`
- `Email__FromAddress`
- `Email__FromName`
- `Email__ReplyToAddress`

Use a Brevo API key, not an SMTP key. Keep the key sealed in the deployment platform and never
place it in source control. `Smtp` remains available for providers and hosting plans that permit
outbound SMTP; `Pickup` remains available for local development.

Alert when dead-letter messages exist or when the age of the oldest pending message exceeds five minutes. The email provider should support domain authentication (SPF, DKIM, and DMARC), delivery event webhooks, and an idempotency key before increasing newsletter volume substantially.
