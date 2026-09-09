import { useEffect, useState } from "react";
import { getSubscriptionStatus, openCustomerPortal, startSubscriptionCheckout } from "../api/subscriptionApi";
import { trackEvent } from "../services/analyticsService";

export default function SubscriptionPage() {
  const [state, setState] = useState(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => { getSubscriptionStatus().then(setState).catch((e) => setError(e.message)); }, []);

  async function redirect(createSession) {
    setBusy(true); setError("");
    try { const data = await createSession(); window.location.assign(data.url); }
    catch (e) { setError(e.message); setBusy(false); }
  }

  if (!state) return <main className="page-shell"><p>{error || "Loading subscription..."}</p></main>;
  return <main className="page-shell"><section className="card subscription-card">
    <p className="subtitle">Artist subscription</p><h1>€14.99 / month</h1>
    <p>Status: <strong>{state.status}</strong></p>
    {state.trialEndsAt && <p>Trial ends: {new Date(state.trialEndsAt).toLocaleDateString()}</p>}
    {state.currentPeriodEndsAt && <p>Current access ends: {new Date(state.currentPeriodEndsAt).toLocaleDateString()}</p>}
    {state.cancelAtPeriodEnd && <p>Your subscription is scheduled to end at the end of the current period.</p>}
    <p>First 3 months from the start of the subscription are free. Card required. No charge today. Applicable taxes are shown by Stripe before confirmation.</p>
    {state.requiresCheckout && <button className="primary-button" disabled={busy} onClick={() => { trackEvent("subscription_checkout_started"); redirect(startSubscriptionCheckout); }}>Add card and start free trial</button>}
    {!state.requiresCheckout && <button className="secondary-button" disabled={busy} onClick={() => redirect(openCustomerPortal)}>Manage billing in Stripe</button>}
    {error && <p className="error">{error}</p>}
  </section></main>;
}
