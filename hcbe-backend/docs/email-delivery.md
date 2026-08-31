# Reliable email delivery

Password-reset and newsletter email is committed to `EmailOutboxMessages` in the same database transaction as the related application state. `EmailOutboxWorker` processes messages in small batches, recovers stale processing locks, and retries failures with exponential backoff. After five failed attempts a message moves to `DeadLetter`.

Administrators can inspect `GET /api/admin/email-outbox` and explicitly retry an unsent message with `POST /api/admin/email-outbox/{id}/retry`. HTML bodies are intentionally omitted from the list response.

Production must configure `Email__Mode=Smtp` and these secrets/settings:

- `Email__Smtp__Host`
- `Email__Smtp__Port`
- `Email__Smtp__EnableSsl`
- `Email__Smtp__Username`
- `Email__Smtp__Password`
- `Email__FromAddress`
- `Email__FromName`

Alert when dead-letter messages exist or when the age of the oldest pending message exceeds five minutes. The SMTP provider should support domain authentication (SPF, DKIM, and DMARC), delivery event webhooks, and an idempotency key before increasing newsletter volume substantially.
