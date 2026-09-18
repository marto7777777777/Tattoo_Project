# InkRoute Stripe AI Project Pass Reliability Fix Report

Date: 2026-09-18
Scope: backend only

## Problem 1 — orphan Stripe Checkout Session / paid-without-entitlement risk

### Root cause
`AiTattooService.CreateCheckoutAsync()` created the Stripe Checkout Session first and only afterwards inserted `AiProjectPayment`. A crash or SQL failure between those steps could leave a valid payable Stripe Session with no local purchase row. The webhook then had no durable server-created intent from which it could safely recover the grant.

### Fix
Added durable `AiProjectCheckoutAttempt` state persisted before Stripe is called.

Each attempt snapshots:
- user and AI project binding;
- product (`ai_project_pass`);
- immutable base amount and currency;
- price semantics (`pre_tax`);
- stable Stripe idempotency key;
- Stripe session id/url/expiry when available;
- monotonic state and timestamps;
- SQL rowversion.

The create flow is now:
1. Short SQL transaction + per-project application lock.
2. Reuse or create one active durable checkout attempt.
3. Commit.
4. Stripe Checkout HTTP call outside SQL using the attempt's stable `Idempotency-Key`.
5. Short SQL transaction attaches the returned Stripe Session.

If Stripe succeeds but the process crashes before the attach, retrying the same checkout reuses the same durable attempt and Stripe idempotency key instead of creating an uncontrolled second purchase.

The webhook can resolve a paid session through signed Stripe metadata containing `checkoutAttemptId`. If the legacy `AiProjectPayment` row is missing but a valid server-created attempt exists, it creates the payment row and grants the pass atomically. Arbitrary metadata without a matching server-created durable intent is never trusted for grant.

Existing pre-migration sessions remain supported through the old `AiProjectPayment` lookup path.

### Duplicate/race protections
- SQL application lock for checkout creation per project.
- Filtered unique index allows at most one active attempt per `UserId + ProjectId + Product`.
- Unique Stripe idempotency key.
- Unique Stripe Checkout Session id on the checkout attempt.
- Existing unique Stripe Checkout Session id on `AiProjectPayment` remains the DB-level double-grant guard.
- Monotonic checkout state transitions.
- Provider webhook event idempotency remains unchanged.

## Problem 2 — Stripe Tax false rejection

### Root cause
Webhook validation compared `session.AmountTotal` with the base configured AI pass amount. With `automatic_tax[enabled]=true`, `AmountTotal` may include VAT/tax while the base product amount remains in `AmountSubtotal`.

### Fix
- The durable attempt snapshots the immutable base amount and currency at checkout creation.
- Stripe dynamic price data is explicitly created with `tax_behavior=exclusive`.
- Webhook validation compares Stripe `AmountSubtotal` to `ExpectedBaseAmountMinor` and independently checks currency.
- `AmountTotal` is no longer incorrectly treated as the base price.
- Stripe remains authoritative for the tax amount; the backend does not recompute VAT manually.
- Future configuration price changes do not invalidate a previously created checkout because validation uses the immutable attempt snapshot.

## Session expiry/retry behavior
A reusable unexpired session returns the existing checkout URL. If the locally known session is expired, the backend first retrieves the session from Stripe outside a SQL transaction. Only a Stripe-confirmed `expired` session is transitioned to terminal `Expired`, after which a new attempt may be created.

A `Creating` attempt does not become permanently stuck: a new request reuses that attempt and the same Stripe idempotency key, allowing recovery from HTTP timeout/process crash.

## Unmatched paid Stripe events
A signed paid event that has neither a durable attempt nor a legacy server-created payment is not granted. It is logged at critical severity with Stripe session/event identity and acknowledged so it cannot create an endless provider retry storm. New checkouts created after this migration cannot enter this state because the durable attempt commits before Stripe is called.

## Files changed / added
- `Models/AiProjectCheckoutAttempt.cs` — new durable attempt/state rules.
- `Configuration/AiProjectCheckoutAttemptConfiguration.cs` — SQL constraints/indexes/rowversion.
- `Data/TattooDbContext.cs` — new DbSet.
- `Services/AiTattooService.cs` — durable checkout, Stripe idempotency/recovery, subtotal validation, webhook reconciliation.
- `Migrations/20260918010000_StripeAiCheckoutReliability.cs` — additive migration.
- `Migrations/TattooDbContextModelSnapshot.cs` — updated model snapshot.
- `Tests/ReliabilityRulesTests.cs` — checkout state-machine tests.
- `Tests/PersistenceContractTests.cs` — persistence/index contract tests.
- Reports/notes updated.

## Business logic preserved
- AI Project Pass duration remains 30 days.
- Project-scoped pass behavior remains unchanged.
- Active pass + separate valid new purchase still extends from the existing end through the existing `AiPassGrantRules`.
- `FreeEditLimit = 0` remains unchanged.
- No frontend files were changed.
- Existing route and response DTO for checkout remain unchanged.
