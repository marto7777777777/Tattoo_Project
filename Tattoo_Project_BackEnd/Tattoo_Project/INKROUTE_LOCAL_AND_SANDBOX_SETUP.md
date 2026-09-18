# InkRoute billing setup (development and sandbox)

No secret or real store product identifier belongs in Git, the frontend, `appsettings.json`, or a mobile binary. Local .NET development uses User Secrets. Production uses environment variables and durable mounted files for provider credentials, certificates, and Data Protection keys.

## Backend environment variables

Use double underscores for nested .NET configuration:

```text
ConnectionStrings__DefaultConnection
AllowedHosts
Jwt__Key
Jwt__Issuer
Jwt__Audience
EmailSettings__SmtpHost
EmailSettings__SmtpPort
EmailSettings__SmtpUsername
EmailSettings__SmtpPassword
EmailSettings__SenderEmail
EmailSettings__SenderName
FrontendUrl
OpenAI__ApiKey
OpenAI__ImageModel
OpenAI__TextModel
Stripe__SecretKey
Stripe__WebhookSecret
Stripe__ArtistMonthlyPriceId
Stripe__CustomerPortalConfigurationId
Stripe__Currency
Stripe__AiProjectPassAmount
Billing__AccountObfuscationSecret
Verification__CodeHashSecret
GooglePlay__PackageName
GooglePlay__ArtistSubscriptionProductId
GooglePlay__ArtistTrialOfferId
GooglePlay__AiProjectPassProductId
GooglePlay__ServiceAccountJsonPath
GooglePlay__PubSubAudience
GooglePlay__PubSubServiceAccountEmail
Apple__BundleId
Apple__ArtistSubscriptionProductId
Apple__AiProjectPassProductId
Apple__IssuerId
Apple__KeyId
Apple__PrivateKeyPath
Apple__RootCertificatePath
DataProtection__KeysPath
DataProtection__CertificatePath
DataProtection__CertificatePassword
Proxy__Enabled
Proxy__ForwardLimit
Proxy__KnownProxies__0
Legal__TermsVersion
Legal__PrivacyVersion
Cors__AllowedOrigins__0
```

In Production, `AllowedHosts` must contain the backend hostname instead of `*`.
Set `Proxy__Enabled=true` only when the backend is behind a reverse proxy, and
list only the immediate trusted proxy IP addresses under `Proxy__KnownProxies__N`.
Do not trust forwarded headers from arbitrary internet clients. When the hosting
provider changes its proxy addresses, update this allow-list before routing traffic.

`Stripe__AiProjectPassAmount` is `1249` EUR cents. It is a one-time 30-day pass for one AI project, never the artist subscription. The committed configuration intentionally contains blank values.

## Frontend public environment variables

```text
VITE_API_BASE_URL
VITE_PUBLIC_APP_URL
VITE_WEB_REGISTER_URL
VITE_APP_STORE_URL
VITE_GOOGLE_PLAY_URL
VITE_LANDING_CANONICAL_URL
VITE_LANDING_OG_IMAGE_URL
VITE_GTM_CONTAINER_ID
VITE_GA4_MEASUREMENT_ID
VITE_META_DATASET_ID
VITE_BUSINESS_PHONE
```

The retired personal Meta Pixel must not be used. GA4 and Meta tags belong in GTM and fire only after the corresponding Consent Mode v2 grant. Do not publish GTM until consent is tested.

## Stripe Sandbox

1. Enable test mode and create a recurring EUR 14.99 monthly artist Price with exclusive tax behavior.
2. Configure Customer Portal for payment method, invoices, cancel-at-period-end and reactivation.
3. Add `/api/stripe/webhook` with `checkout.session.completed`, `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.paid`, and `invoice.payment_failed`.
4. Store the signing secret only in User Secrets/environment variables.
5. Test completed and abandoned Checkout, trial cancellation, renewal, failed invoice, duplicate delivery and portal reactivation. Access must change only after a verified webhook.

## Google Play (after approval)

1. Keep package `com.inkroute.app`. Create the monthly base plan, eligible three-month offer and a separate consumable one-time AI pass product.
2. Grant an isolated service account Android Publisher API access. Mount JSON outside the app and configure the absolute path.
3. Configure authenticated Pub/Sub push to `/api/subscription/webhooks/google`; set exact OIDC audience and service-account email.
4. Test trial, renewal, cancel-valid-until-expiry, grace, hold, pause, expiry, refund/revocation, pending, restore and token reuse.

Android uses Billing Library 9.1.0. Acknowledgement is backend-only after verification.

## Apple (after approval)

1. Keep bundle `com.inkroute.app`. Create the auto-renewable monthly subscription with eligible introductory offer and a separate consumable AI pass IAP.
2. Mount the App Store Server API `.p8` outside the app and configure issuer ID, key ID and absolute path.
3. Mount the official Apple Root CA read-only and configure its absolute path.
4. Configure Server Notifications V2 to `/api/subscription/webhooks/apple` for Sandbox and Production.
5. Test purchase, pending/cancelled, restore, renewal, retry, grace, cancellation, expiry, refund/revocation and account-token mismatch.

The backend validates JWS/x5c, bundle, product, environment and account token, queries the Server API with production-to-sandbox fallback, and updates the common entitlement. The client finishes only after backend verification.

## Data Protection

`DataProtection__KeysPath` must be an absolute durable shared directory used by every backend instance. Protect its key ring with an X.509 certificate supplied through secret storage.

Tattoo-request and AI images are private. The API returns short-lived signed read
URLs, while direct `/uploads/tattoo-request-images/*` and `/uploads/ai-tattoos/*`
requests are intentionally rejected. Profile, portfolio and studio presentation
images remain public.

## Commands

```bash
dotnet restore
dotnet build -c Release
dotnet run --project ReleaseTests/InkRoute.Backend.ReleaseTests.csproj -c Release
dotnet ef database update
npm ci
npm run build
npm audit --omit=dev
cd android && ./gradlew assembleDebug
```

iOS compilation and StoreKit tests require macOS/Xcode. Verify `InkRouteBridgeViewController` in the storyboard and both Swift files in the App target. Legal versions are `2026-09-10`; every `TODO: LEGAL REVIEW` needs counsel before production.
