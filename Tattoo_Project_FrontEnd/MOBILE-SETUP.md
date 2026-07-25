# InkRoute mobile setup

InkRoute uses the existing React application in three forms:

1. Regular responsive website.
2. Installable Progressive Web App (PWA).
3. Native Android and iOS projects powered by Capacitor.

No business workflow, AI limit, Stripe rule, booking rule, or backend endpoint is
duplicated in the mobile projects.

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

## iOS

iOS native builds require macOS with Xcode. On that Mac run:

```bash
npm install
npm run mobile:ios
```

Then choose your Apple signing team and the connected iPhone in Xcode.

## After every frontend change

Run:

```bash
npm run mobile:sync
```

This creates a fresh web build and copies it into both native projects.
