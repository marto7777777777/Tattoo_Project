# Backend / Frontend Notes

No frontend change is required for the Stripe reliability fix.

The existing AI checkout endpoint and `CheckoutSessionDto { Url }` response contract remain unchanged. When the same checkout is retried while its Stripe Session is still usable, the backend may return the existing Checkout URL instead of creating another session. This is intentionally transparent to the current frontend.

No React/Vite/PWA/Capacitor files were modified.
