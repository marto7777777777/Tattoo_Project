# InkRoute Backend Deployment

## Required runtime
Use .NET 10 and SQL Server. Build/migration tooling requires the .NET 10 SDK.

## Configuration
Copy `appsettings.example.json` and provide secrets through environment variables/secret mounts. Do not commit production credentials.

Production requires SQL, JWT, verification secret, frontend/backend HTTPS URLs, SMTP, legal versions, durable Data Protection keys and durable media storage. Provider settings are required only when the corresponding `PaymentProviders:*Enabled` flag is true.

### Stripe
Create a separate active, one-time (non-recurring) EUR 12.49 Stripe Price for the AI Project Pass and configure its ID as `Stripe:AiProjectPassPriceId`. Keep `Stripe:AiProjectPassAmount` at exactly `1249` euro cents as an independent server-side verification value. Configure the backend webhook secret and the separate recurring artist monthly Price. Never grant entitlement from a success/return URL; AI access is granted only after a verified paid webhook.

### Google Play
Configure package name, artist subscription product, `ArtistBasePlanId`, deterministic trial offer ID, AI consumable product, service-account file, Pub/Sub audience and Pub/Sub service-account email.

### Apple
Configure bundle ID, artist subscription product, AI consumable product, issuer/key IDs, private key and trusted root certificate. Configure App Store Server Notifications to the backend.

## Database migration
Do not run destructive migrations automatically at startup.

```bash
dotnet restore
dotnet build -c Release
dotnet ef migrations script --idempotent -o inkroute-migrations.sql
```

Review the SQL and back up production before applying. The new migration is `20260917130000_FinalBackendHardening`.

## Storage / Data Protection
`Storage:RootPath` and `DataProtection:KeysPath` must be absolute, persistent and writable by the deployed Docker `app` user. Multi-instance deployments must share the same backing store. Private media is not intended to be exposed as static files.

## Proxy / CORS
If behind a proxy, enable `Proxy:Enabled` and configure explicit trusted proxy IPs. Do not trust arbitrary forwarded headers. Restrict `AllowedHosts` and include the exact frontend origin in `Cors:AllowedOrigins`.

## Health
- `GET /health/live`
- `GET /health/ready` (includes DB connectivity)

## Release commands
```bash
dotnet restore
dotnet build -c Release
dotnet run --project ReleaseTests -c Release
dotnet test -c Release
dotnet ef migrations script --idempotent -o inkroute-migrations.sql
dotnet list package --vulnerable --include-transitive
docker build -t inkroute-backend:release .
```

## Rollback
Restore the previous application image first. For DB rollback, take a backup and generate/review an EF migration script to the previous known migration. Preserve payment anti-replay/tombstone records during rollback planning.

## External sandbox verification
Before live deployment, verify Stripe webhooks, Google subscription/base-plan/trial + consumable consume/retry + RTDN, Apple sandbox purchase/replay/app-account binding + notifications, and OpenAI timeout/error behavior with real provider credentials.
