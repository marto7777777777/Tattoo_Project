# InkRoute local and Stripe Sandbox setup

No credentials belong in tracked files. Use .NET User Secrets locally and environment variables in hosted environments.

## Backend configuration keys

Set these as User Secrets using the colon form shown below. In production, replace each `:` with `__`.

```text
ConnectionStrings:DefaultConnection
Jwt:Key
Jwt:Issuer
Jwt:Audience
Jwt:ExpiresInMinutes
EmailSettings:SmtpHost
EmailSettings:SmtpPort
EmailSettings:SmtpUsername
EmailSettings:SmtpPassword
EmailSettings:SenderEmail
EmailSettings:SenderName
FrontendUrl
OpenAI:ApiKey
OpenAI:ImageModel
OpenAI:TextModel
OpenAI:PlannerMaxOutputTokens
Stripe:SecretKey
Stripe:WebhookSecret
Stripe:ArtistMonthlyPriceId
Stripe:CustomerPortalConfigurationId
Stripe:Currency
Stripe:AiProjectPassAmount
Legal:TermsVersion
Legal:PrivacyVersion
Cors:AllowedOrigins:0
AdminSeed:Email
AdminSeed:Password
AdminSeed:UserName
AdminSeed:FirstName
AdminSeed:LastName
```

Example command structure (provide your own value; do not commit it):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_LOCAL_VALUE"
dotnet user-secrets set "Jwt:Key" "YOUR_LOCAL_VALUE"
dotnet user-secrets set "Stripe:SecretKey" "YOUR_TEST_MODE_VALUE"
dotnet user-secrets set "Stripe:WebhookSecret" "YOUR_LOCAL_FORWARDING_VALUE"
```

The SMTP username is the real Google Workspace mailbox. The sender may be an approved alias. Revoke the previously exposed Gmail App Password and generate a new one before local testing.

## Frontend public configuration

Copy `.env.example` to an untracked `.env.local`. These are public browser values only:

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

Never place Stripe secret keys, webhook secrets, database credentials, JWT signing keys, SMTP passwords, or OpenAI API keys in a frontend environment file.

## Database and local run

```bash
dotnet restore
dotnet ef database update
dotnet run
```

In the frontend directory:

```bash
npm ci
npm run dev
```

## Stripe Sandbox manual configuration

1. Enable Stripe test mode.
2. Create an InkRoute artist subscription product and one recurring monthly price for EUR 14.99. Configure tax behavior as exclusive so applicable tax is added separately. Put the resulting test price ID in `Stripe:ArtistMonthlyPriceId`.
3. Complete the Stripe Tax test-mode business-origin and registration settings applicable to your test scenarios. Checkout uses Automatic Tax and a required billing address; verify the final tax-inclusive total appears before confirmation.
4. Configure Customer Portal in test mode. Allow payment-method updates, invoice/payment-history viewing, cancellation at period end, and subscription reactivation when Stripe makes it applicable. Save the test configuration ID in `Stripe:CustomerPortalConfigurationId`, or leave it empty to use the account default portal configuration.
5. Create a test webhook endpoint ending in `/api/stripe/webhook` and subscribe it to:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.paid`
   - `invoice.payment_failed`
6. Store the test endpoint signing secret only in `Stripe:WebhookSecret` through User Secrets or an environment variable.
7. For local forwarding, use Stripe CLI and set its temporary signing secret as the local User Secret:

```bash
stripe listen --forward-to https://localhost:7115/api/stripe/webhook
```

8. Register an artist, finish the profile, and complete Checkout with a Stripe test card. Confirm that the database remains `pending_subscription` after a cancelled Checkout and becomes `trialing` only after the signed webhook.
9. Use Stripe test clocks or Dashboard test events to validate the three-calendar-month transition, first and second paid invoices, payment failure, cancel-at-period-end, reactivation, and final deletion.
10. Confirm that replaying the same event ID does not create a second milestone, payment count, or analytics event.

Do not switch to live keys, publish a GTM container, or deploy this build without explicit approval.

## GTM / Consent Mode test configuration

Keep the GTM container unpublished while testing. Configure GA4 and the InkRoute-owned Meta Dataset inside GTM, never by adding Meta Pixel directly to the page. Require `analytics_storage` for GA4 and marketing/ad consent for Meta. Do not configure any legacy personal Pixel.

Map only the allowlisted `dataLayer` events and the single safe parameter `completed_project_count`. Do not create variables for names, email addresses, phone numbers, application IDs, Stripe IDs, tattoo descriptions, messages, images, health data, or free text.

Test all three consent paths before publishing anything: Accept all, Reject non-essential, and custom preferences. Also test changing the choice from the footer.
