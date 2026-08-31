# Newsletter HCBE — Design Spec

**Date:** 2026-07-12  
**Status:** Approved for implementation planning  
**Scope:** V1 — public signup + admin list/export (no campaign sending)

## Goal

Allow visitors to subscribe to the HCBE newsletter (email, first name, language) with CASL-compliant consent, and allow admins to view, filter, deactivate, and export subscribers.

## Decisions

| Topic | Choice |
|---|---|
| Scope | Signup + admin list/export |
| Placement | Home dedicated banner + compact footer on all public pages |
| Fields | Email, first name, preferred language (fr/en) |
| Consent | Required checkbox + link to privacy policy |
| Architecture | HCBE backend DB as source of truth |
| Home UI | Dedicated full-width section before final CTA |
| Out of scope V1 | Campaign sending, double opt-in email, ESP sync (Brevo/Mailchimp) |

## Architecture

```
[NewsletterSignup form] --POST--> /api/newsletter/subscribe (anonymous)
                                        |
                                        v
                              NewsletterSubscriptions (DB)
                                        |
[Admin auth] --GET/PATCH/EXPORT--> /api/newsletter/subscriptions
```

- Backend follows existing patterns (model, service, endpoints, EF + ensure-table like membership applications).
- Frontend reuses one `NewsletterSignup` component with `variant: 'home' | 'footer'`.
- Replace/remove unused `NewsletterSection` that posts to Readdy.

## Data model

`NewsletterSubscription`

| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Email | string | Unique, normalized lowercase |
| FirstName | string | Required |
| PreferredLanguage | string | `fr` or `en` |
| ConsentAcceptedAt | DateTime | Set only when consent checkbox is true |
| IsActive | bool | Soft unsubscribe / admin deactivation |
| Source | string | `home` or `footer` |
| CreatedAt | DateTime | UTC |
| UpdatedAt | DateTime | UTC |

### Subscribe semantics

1. New email → create active subscription.
2. Existing active email → return generic success (no email enumeration).
3. Existing inactive email → reactivate, update first name / language / consent timestamp / source.
4. Missing consent → 400 validation error.

## API

### Public

`POST /api/newsletter/subscribe`

```json
{
  "email": "string",
  "firstName": "string",
  "preferredLanguage": "fr|en",
  "consentAccepted": true,
  "source": "home|footer"
}
```

Response: `200` with generic success message payload (always same for duplicate active).

### Admin (authenticated)

- `GET /api/newsletter/subscriptions?language=&isActive=`
- `PATCH /api/newsletter/subscriptions/{id}` — body `{ "isActive": boolean }`
- `GET /api/newsletter/subscriptions/export` — CSV of active subscribers

## Frontend

### Public

- `NewsletterSignup` component (i18n FR/EN): first name, email, language select, consent checkbox + privacy link, submit.
- Home: new section before `CTASection`.
- Footer: compact inline variant under the brand/description block (same column as institutional flags).
- Privacy page at `/confidentialite` with bilingual content via i18n (no separate EN URL required).

### Admin

- Route `/admin/newsletter`
- Table: first name, email, language, source, created date, status
- Filters: language, active/inactive
- Actions: deactivate, export CSV
- Nav entry in admin layout

## Error handling

| Case | Behavior |
|---|---|
| Invalid fields / no consent | Inline validation + 400 |
| Network/server error | Clear error toast/message, keep form values |
| Success (new or duplicate active) | Same success message |
| Reactivation | Same success message |

## Security

- Public subscribe endpoint: strict validation; no subscriber list exposure.
- Admin endpoints: existing auth/authorization.
- Consent timestamp stored; privacy policy linked from form.
- Rate limiting / honeypot deferred to V1.1 unless trivial to add with existing middleware.

## Testing

- Service unit tests: create, duplicate active, reactivate inactive, reject without consent.
- API tests: subscribe validation, admin auth required for list/export.
- Frontend smoke: form validation + success state (home or footer).

## Non-goals (explicit)

- Sending newsletter campaigns from the admin UI
- Double opt-in confirmation emails
- Automatic sync to Brevo/Mailchimp
- Integrating newsletter preference into member accounts (can be a later link)

## Implementation notes

- Prefer failing fast on invalid language/source enums.
- Normalize email to lowercase before uniqueness checks.
- CSV export: UTF-8 with header row suitable for import into an ESP later.
- Keep copy institutional and bilingual; reuse emerald brand language of the public site.
