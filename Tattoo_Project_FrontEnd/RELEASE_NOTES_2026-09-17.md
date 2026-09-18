# InkRoute frontend production release

- Registration now retrieves authoritative legal versions from `GET /api/legal/versions`, blocks submission until they load, and offers retry on failure.
- AI Project Pass supports paid draft creation before checkout, web Stripe checkout, native Google Play/App Store purchase verification, and backend-authoritative generation/edit permissions.
- Google Play offer selection is deterministic by product, base plan, and trial offer; native prices come from store product details.
- Analytics and marketing tags are consent-gated. YouTube embeds load only after a user click and use privacy-enhanced mode.
- Cloudflare Pages security headers and CSP are included in `public/_headers`.
- Production API default: `https://api.inkroute.app`.

Verification performed: ESLint (zero warnings), Vite production build, npm production dependency audit (zero known vulnerabilities), Capacitor Android/iOS sync. Native Gradle compilation requires network access to download the configured Gradle distribution.
