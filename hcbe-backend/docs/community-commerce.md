# Community ticketing, organizers, and advertising

HCBE supports free and paid event tickets, QR admission, organizer payouts through Stripe Connect, and moderated community advertising. Card data is collected only by Stripe-hosted Checkout; HCBE stores orders, financial totals, ticket state, and the provider identifiers required for refunds and reconciliation.

## Operating model

- HCBE events settle into the platform Stripe account.
- Approved community organizers complete Stripe-hosted onboarding and sell using direct charges on their connected account.
- The configured platform fee is deducted by Stripe. New organizer events default to 5%, configurable with `CommunityMarketplace__PlatformFeePercent` and capped by the API at 25%.
- An organizer event stays in `Draft` until an administrator reviews and publishes it.
- Advertising submissions stay in `Submitted` until an administrator approves them. Public placements always label them as sponsored content.
- Approved campaigns can appear on the homepage, news, services, and event listings. The platform records aggregate impressions and outbound clicks.

## Stripe Connect configuration

The integration creates Accounts v2 merchant accounts with the full Stripe Dashboard and Stripe-hosted onboarding. Do not collect identity, banking, or verification documents in HCBE. The legal entity type is deliberately left to onboarding so an individual, nonprofit, or registered business can provide its accurate status directly to Stripe.

Configure staging with Stripe test-mode credentials and production with separate live credentials:

```text
Finance__Enabled=true
Finance__SecretKey=<restricted Stripe platform key>
Finance__WebhookSecret=<platform endpoint whsec_...>
Finance__ConnectWebhookSecret=<connected-accounts endpoint whsec_...>
CommunityMarketplace__PlatformFeePercent=5
```

Both webhook endpoints may use the same HCBE URL, but Stripe issues a distinct signing secret for each endpoint scope:

```text
https://api.hcbe.ca/api/finance/webhooks/stripe
https://api-staging.hcbe.ca/api/finance/webhooks/stripe
```

Subscribe platform and connected-account delivery to:

- `checkout.session.completed`
- `checkout.session.async_payment_succeeded`
- `checkout.session.async_payment_failed`
- `checkout.session.expired`
- `charge.refunded`
- `charge.dispute.created`

Keep the existing membership/subscription events on the platform endpoint. Rotate API and signing secrets independently in Stripe and Railway. Never copy a test secret into production.

Before enabling community sales, activate the platform's Connect profile, confirm the controller and fee/loss configuration with HCBE's accountant, and complete one end-to-end test including onboarding, sale, ticket PDF, scan, partial/full refund, payout, dispute visibility, and reconciliation.

## Event and admission workflow

1. An administrator enables ticketing on an event or an approved organizer creates a draft festival.
2. Add one or more ticket tiers, inventory, sale dates, per-order limits, and optional promotion codes.
3. Publish the event only after dates, bilingual content, organizer, refund terms, accessibility, and capacity are verified.
4. Checkout reserves no permanent inventory until the signed success webhook arrives. Overselling is rejected again during fulfillment.
5. Paid or free orders receive unique bearer access links and individual QR ticket codes.
6. At admission, staff use the administration check-in control. A second scan reports the ticket as already used rather than admitting it twice.
7. Refunds are initiated from the administration order view and confirmed by webhook before the HCBE ledger changes.

## Advertising controls

- Accept only HTTPS destination and image URLs.
- Verify advertiser identity, destination safety, dates, language, region, creative rights, and claims before approval.
- Reject political, discriminatory, deceptive, illegal, privacy-invasive, or community-inappropriate content.
- Keep sponsorship visibly labelled in both languages; do not style an advertisement as an HCBE editorial endorsement.
- Aggregate impressions and clicks only. Do not add cross-site tracking, fingerprinting, or behavioural profiling without a separate privacy/legal review and explicit consent design.
- Pause expired, disputed, unsafe, or withdrawn campaigns immediately.

## Privacy, retention, and support

The Law 25 account export includes ticket purchases, issued tickets, organizer-profile data, and submitted campaigns. Account deletion cancels optional communications, detaches and anonymizes ticket identities, rotates ticket access tokens, suspends organizer sales, and pauses advertising. Minimal payment/order records and connected-account identifiers remain only for refunds, disputes, accounting, and statutory retention.

Publish bilingual ticket/refund terms and organizer/advertising terms before accepting live community money. Document the retention period and lawful basis for financial records with HCBE's privacy officer and accountant. This runbook is an operational control, not legal or tax advice.
