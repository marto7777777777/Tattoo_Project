# InkRoute Stripe Reliability Test Report

Date: 2026-09-18

## Environment
The execution environment used for this patch does not provide the .NET SDK or Docker CLI.

## Commands
| Command | Result | Notes |
|---|---|---|
| `dotnet restore` | NOT RUN | `dotnet` executable unavailable |
| `dotnet build -c Release` | NOT RUN | `dotnet` executable unavailable |
| `dotnet test -c Release` | NOT RUN | `dotnet` executable unavailable |
| `dotnet run --project ReleaseTests -c Release` | NOT RUN | `dotnet` executable unavailable |
| `dotnet ef migrations script --idempotent -o inkroute-migrations.sql` | NOT RUN | `dotnet` executable unavailable |
| `dotnet list package --vulnerable --include-transitive` | NOT RUN | `dotnet` executable unavailable |
| `docker build -t inkroute-backend:release .` | NOT RUN | `docker` executable unavailable |

No build/test command is represented as successful when it was not executed.

## Static checks actually performed
- Source/migration brace and parenthesis balance for newly edited core files: PASS.
- Confirmed `FreeEditLimit = 0`: PASS.
- Confirmed old AI Stripe `AmountTotal == AmountInMinorUnits` validation is removed: PASS.
- Confirmed durable `AiProjectCheckoutAttempts` DbSet/model/configuration/migration/snapshot are present: PASS.
- Confirmed stable Stripe `Idempotency-Key` is generated from the durable attempt and reused: PASS by code review.
- Confirmed Checkout Session HTTP call occurs outside SQL transaction: PASS by code review.
- Confirmed filtered unique active-attempt index exists: PASS by source/migration review.
- Confirmed Stripe Checkout Session remains unique on `AiProjectPayment`: PASS by model configuration review.
- Confirmed no `bin`, `obj`, or `.git` directories were found before packaging: PASS.
- ZIP integrity: performed after packaging; see final handoff.

## Automated tests added
- `AiStripeCheckout_StateMachine_IsMonotonic`
- `AiStripeCheckout_OnlyFinanciallyLiveStatesAreActive`
- `AiStripeCheckoutAttempt_HasDurableIdempotencyAndSingleActiveAttemptConstraints`
- `AiProjectPayment_StripeSessionRemainsUniqueForDoubleGrantProtection`

These tests are added to the test project but could not be executed in this environment because .NET is unavailable.

## Runtime tests still required before production deployment
Run the commands above in an environment with .NET 10 + SQL Server. In particular verify:
- duplicate checkout creation under concurrency;
- Stripe success followed by simulated local DB attach failure;
- retry with same Stripe idempotency key;
- webhook before local Stripe Session attach;
- VAT/exclusive-tax session where `AmountTotal > AmountSubtotal`;
- duplicate/different Stripe success events for the same Checkout Session;
- temporary DB failure after paid webhook followed by webhook retry;
- session expiry verification and replacement;
- active pass extension by one distinct paid transaction exactly once.
