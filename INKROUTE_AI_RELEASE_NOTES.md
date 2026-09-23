# InkRoute AI reliability and Stripe Price release

## Required production setting

Create an active Stripe Price with these exact properties:

- Type: one-time (not recurring)
- Currency: EUR
- Unit amount: EUR 12.49
- Tax behavior: exclusive

Add its `price_...` identifier to the Azure App Service application settings:

```text
Stripe__AiProjectPassPriceId=price_...
```

Keep the existing verification value:

```text
Stripe__AiProjectPassAmount=1249
```

Restart the App Service after saving the setting. Do not put either the Stripe secret key or webhook secret in source control.

## Behavior preserved

- AI Project Pass Checkout uses Stripe `payment` mode, never `subscription` mode.
- Access is granted only by the verified paid Stripe webhook.
- The durable checkout attempt and Stripe idempotency key still prevent duplicate sessions and duplicate grants.
- Existing open sessions created by the previous release remain reconcilable through their session, amount, currency and project bindings.
- The AI generation lock remains active; the frontend now reconciles an interrupted request instead of starting a duplicate operation.

## Deployment checks

1. Deploy the backend after adding `Stripe__AiProjectPassPriceId`.
2. Deploy the frontend so the updated Cloudflare `_headers` policy is active.
3. Confirm the response header for `https://inkroute.app` contains `https://api.inkroute.app` in `img-src`.
4. Create a paid AI draft and confirm Checkout shows a one-time EUR 12.49 purchase.
5. Complete payment and wait for the webhook before generating.
6. Generate an image and confirm it displays inside the app, not only in a separate tab.
