# InkRoute frontend

Production frontend for InkRoute web/PWA, Android and iOS. The backend is the source of truth for authentication, subscription entitlement, AI access and purchase verification.

## Run

```bash
npm ci
npm run dev
```

## Notes

## Production checks

```bash
npm run lint
npm run build
npm audit
npx cap sync
```

Only public `VITE_` values belong in frontend environment files. Store product IDs come from the backend. Never add Stripe secrets, Apple keys, Google service-account credentials, JWT keys or OpenAI keys here.

Cloudflare builds with `npm ci && npm run build` and serves `dist`; `wrangler.jsonc` provides the SPA fallback.
