# Deployment Notes — Stripe AI Checkout Reliability

## Migration
Apply the new additive migration:

`20260918010000_StripeAiCheckoutReliability`

Normal deployment command:

```bash
dotnet ef database update
```

Generate/review the idempotent script before production deployment:

```bash
dotnet ef migrations script --idempotent -o inkroute-migrations.sql
```

The migration adds only `AiProjectCheckoutAttempts` and indexes/constraints. Existing `AiProjectPayments` and entitlements are not deleted or rewritten.

## Stripe behavior
AI Project Pass checkout now uses:
- `automatic_tax[enabled]=true`
- dynamic price data with `tax_behavior=exclusive`
- immutable base price snapshot in SQL
- server-generated Stripe idempotency key per durable attempt

The current base amount remains unchanged in code. No pricing change was introduced.

## Webhook configuration
No webhook secret/config key changes are required for these two fixes. Existing signed Stripe webhook validation remains authoritative.

## Rollout recommendation
1. Back up production DB.
2. Apply migration.
3. Deploy backend.
4. Perform a Stripe test-mode AI Project Pass checkout with tax enabled.
5. Confirm Stripe subtotal equals the server base snapshot while total may include tax.
6. Confirm one paid session extends the project exactly once.
7. Replay the same event and verify no second extension.

## Rollback
Rolling application code back after the additive migration is generally schema-compatible because the new table is not required by old code. Do not drop the new table while new checkout attempts may be in flight. A full schema rollback should only occur after confirming there are no financially relevant attempts requiring reconciliation.
