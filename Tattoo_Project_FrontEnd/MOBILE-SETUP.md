# InkRoute mobile setup

InkRoute uses the existing React application in three forms:

1. Regular responsive website.
2. Installable Progressive Web App (PWA).
3. Native Android and iOS projects powered by Capacitor.

No business workflow, AI limit, payment rule, booking rule, or backend endpoint is
duplicated in the mobile projects.

Digital purchases are platform-aware: Stripe Checkout on web/PWA, Google Play Billing on Android and StoreKit 2 on iOS. Product IDs, trial eligibility and account-binding values are loaded from the backend. The native bridge never grants entitlement locally and finishes/acknowledges a transaction only after successful server verification.

## API configuration

Browser development continues to use `https://localhost:7115` by default.

For a real phone or a production build, create `.env.production`:

```env
VITE_API_BASE_URL=https://api.your-domain.com
```

The address must point to the deployed ASP.NET API over trusted HTTPS.
`localhost` on a phone points to the phone itself, not to the development PC.

## PWA test

```bash
npm install
npm run build
npm run preview
```

The service worker is generated only for a production build. The app shell is
available offline, while API requests always stay network-only to avoid showing
stale booking, payment, session, or AI data.

## Android

Install Android Studio and its Android SDK, then run:

```bash
npm install
npm run mobile:android
```

This rebuilds React, synchronizes it into the native Android project, and opens
the project in Android Studio.

Before release, create the monthly subscription and consumable AI Project Pass in Google Play Console, configure their IDs in the backend, upload an Internal Testing build and test purchase, pending, cancellation, restore and renewal flows with license testers.

## iOS

iOS native builds require macOS with Xcode. On that Mac run:

```bash
npm install
npm run mobile:ios
```

Then choose your Apple signing team and the connected iPhone in Xcode.

The Xcode target includes `InkRouteBillingPlugin.swift` and `InkRouteBridgeViewController.swift`. Create the auto-renewable subscription and consumable AI Project Pass in App Store Connect, configure their IDs in the backend and validate the flows in Sandbox/TestFlight before release.

## After every frontend change

Run:

```bash
npm run mobile:sync
```

This creates a fresh web build and copies it into both native projects.
