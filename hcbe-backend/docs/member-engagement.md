# Member engagement and retention

The member workspace provides a private, account-scoped engagement layer:

- `GET /api/member-engagement/dashboard` aggregates upcoming registrations, recommended opportunities, saved content, unread messages, service cases, and private notifications.
- Members can save published events and opportunities through `/api/member-engagement/saved/{type}/{id}`.
- Members can block or unblock another member through `/api/member-engagement/blocks/{memberId}`. A block in either direction prevents new conversations and messages.
- Member notifications are strictly scoped to their `UserId`; global notifications with a null owner remain administrator-only.
- Event reminders run every six hours for confirmed registrations starting within 25 hours. In-app reminders are automatic; email reminders respect `EmailEvents`.
- The weekly community digest is disabled by default and requires the member to explicitly choose `Weekly` in communication preferences.
- Event confirmation codes can be presented as QR codes and checked in by an authorized administrator.

## Operational notes

`MemberEngagementWorker` is idempotent for event reminders through `EventRegistration.ReminderSentAt`. Weekly digests use `MemberPreference.LastDigestSentAtUtc` and are sent no more than once every seven days. All emails enter the reliable email outbox before delivery.

The `AddMemberEngagementRetention` migration must be applied before deploying the API. SQLite development databases are upgraded by the compatibility schema routine during startup.
