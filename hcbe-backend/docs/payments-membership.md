# Payments, memberships, and contributions

HCBE uses Stripe-hosted Checkout for card entry and keeps its own auditable ledger in PostgreSQL. The application never receives or stores card numbers. Payment status changes are accepted only through signed Stripe webhooks; the browser success page is informational and cannot mark a transaction paid.

## Production configuration

Create a restricted Stripe key with only the permissions required for Checkout Sessions, Customers, Billing Portal sessions, Subscriptions, Payment Intents, and Refunds. Store it as a sealed Railway variable. Do not place live keys in source control or frontend variables.

```text
Finance__Enabled=true
Finance__Provider=Stripe
Finance__SecretKey=<restricted Stripe key>
Finance__WebhookSecret=<whsec_... signing secret>
Finance__AutomaticTaxEnabled=false
Finance__MembershipGracePeriodDays=30
Finance__MinimumDonationCents=500
Finance__Currency=cad
```

Keep `Finance__AutomaticTaxEnabled=false` until HCBE has confirmed its tax registration and product tax treatment with its accounting or legal adviser. Payment receipts deliberately state that they are not charitable tax receipts.

Create the webhook endpoint at:

```text
https://api.hcbe.ca/api/finance/webhooks/stripe
```

Set the webhook endpoint API version to `2026-06-24.dahlia`, matching the pinned
Stripe.net 52.1.x SDK, and subscribe it only to these events:

- `checkout.session.completed`
- `checkout.session.async_payment_succeeded`
- `checkout.session.async_payment_failed`
- `checkout.session.expired`
- `invoice.paid`
- `invoice.payment_failed`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `charge.refunded`
- `charge.dispute.created`

The webhook signing secret is different from the API key. Rotate either secret in Railway and Stripe together. Configure Stripe's Customer Portal so members can update a payment method and cancel recurring billing; HCBE opens this hosted portal from the member space.

## First-time setup

1. Apply `AddCommunityFinance` to PostgreSQL through the existing API pre-deploy migration command.
2. In **Administration → Finances**, create at least one membership plan. Choose `Annual` for a one-time yearly payment or `Recurring` for automatic annual renewal.
3. A Stripe Price ID is optional. When supplied, it must match the plan's CAD amount and recurrence. Without one, Checkout creates inline price data.
4. Create and publish contribution campaigns. Unpublished or out-of-date campaigns are never offered publicly.
5. Complete a small live payment, confirm its webhook is marked processed, download the HCBE receipt, then refund it from the admin ledger.
6. Confirm the refund and any test subscription cancellation arrive as webhooks and are reflected in the ledger.

## Local webhook testing

Keep local payments disabled unless a developer is deliberately testing Stripe. With Stripe CLI authenticated, set a test restricted key and forward events:

```text
stripe listen --forward-to http://localhost:8080/api/finance/webhooks/stripe
```

Copy the temporary `whsec_...` value into `Finance__WebhookSecret`, use Stripe test mode, and never reuse production secrets locally.

## Operational controls

- Checkout and refund API calls use deterministic idempotency keys, and
  `PaymentWebhookEvents.ProviderEventId` is unique. A webhook delivery is claimed
  before its business changes are applied; failed handlers discard all tracked
  changes and remain retryable.
- Delayed payment methods remain pending after Checkout and activate a membership
  only after `checkout.session.async_payment_succeeded`.
- The initial subscription invoice is attached to its Checkout transaction and cannot be counted again as a renewal.
- Failed renewals place membership in a grace period and alert both the member and administrators.
- Partially refunded contributions remain in campaign totals at their net amount.
- Full refunds of the latest membership transaction make the standing inactive
  and cancel its automatic renewal. A failed Stripe cancellation raises a finance
  alert for manual follow-up.
- Refunds that Stripe reports as pending or requiring action do not change the
  HCBE ledger until the signed provider webhook confirms the refunded amount.
- Disputes are visible in the finance ledger and generate an administrator alert.
- Financial CSV export is restricted to administrators with `finance.manage`.
- Account deletion anonymizes payer identity while preserving the minimal accounting entry required for reconciliation and legal retention.

Reconcile the HCBE ledger against Stripe at least monthly and after any dispute. Investigate unmatched amounts, missing webhook events, failed email receipts, and refunds before closing the period.
