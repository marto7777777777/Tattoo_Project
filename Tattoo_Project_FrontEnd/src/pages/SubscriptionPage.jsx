import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { getSubscriptionStatus } from "../api/subscriptionApi";
import { billingPlatform, getArtistProductDetails, manageArtistSubscription, purchaseArtistSubscription, restoreArtistSubscription } from "../services/billingService";
import { trackEvent } from "../services/analyticsService";
import { useLanguage } from "../i18n/LanguageContext";

const wait = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds));

export default function SubscriptionPage() {
  const { language } = useLanguage();
  const bg = language === "bg";
  const [params, setParams] = useSearchParams();
  const [state, setState] = useState(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [storeProduct, setStoreProduct] = useState(null);
  const platform = billingPlatform();
  const load = useCallback(async () => { const status = await getSubscriptionStatus(); setState(status); return status; }, []);
  useEffect(() => { getArtistProductDetails().then(setStoreProduct).catch(() => {}); }, []);
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const returned = platform === "web" && (params.has("checkout") || params.has("success") || params.has("session_id"));
        let status;
        for (let attempt = 0; attempt < (returned ? 6 : 1); attempt += 1) {
          status = await getSubscriptionStatus();
          if (status.hasAccess || !returned) break;
          await wait(1500 * (attempt + 1));
        }
        if (!cancelled) {
          setState(status);
          if (returned) {
            setMessage(status.hasAccess ? (bg ? "Абонаментът е потвърден." : "Your subscription is confirmed.") : (bg ? "Плащането още се потвърждава. Не купувай повторно; презареди след малко." : "Payment is still being confirmed. Do not purchase again; refresh shortly."));
            setParams({}, { replace: true });
          }
        }
      } catch (e) { if (!cancelled) setError(e.message); }
    })();
    return () => { cancelled = true; };
  }, [bg, params, platform, setParams]);

  async function run(operation, successMessage) {
    setBusy(true); setError(""); setMessage("");
    try { const result = await operation(); if (result?.pending) setMessage(bg ? "Покупката чака потвърждение. Не купувай повторно." : "The store purchase is pending. Do not purchase again."); else if (!result?.redirected) { await load(); setMessage(successMessage); } }
    catch (e) { setError(e.message); }
    finally { setBusy(false); }
  }

  if (!state) return <main className="page-shell"><p>{error || (bg ? "Зареждане на абонамента..." : "Loading subscription...")}</p></main>;
  const date = (value) => new Date(value).toLocaleDateString(language === "bg" ? "bg-BG" : undefined);
  return <main className="page-shell"><section className="card subscription-card">
    <p className="subtitle">{bg ? "Абонамент за татуисти" : "Artist subscription"}</p><h1>{storeProduct?.formattedPrice || "€14.99"} / {bg ? "месец" : "month"}</h1>
    <p>{bg ? "Статус" : "Status"}: <strong>{state.status}</strong></p>
    {state.activeProvider && <p>{bg ? "Платежен доставчик" : "Payment provider"}: <strong>{state.activeProvider}</strong></p>}
    {state.trialEndsAt && <p>{bg ? "Безплатният период приключва" : "Trial ends"}: {date(state.trialEndsAt)}</p>}
    {state.currentPeriodEndsAt && <p>{bg ? "Текущият период приключва" : "Current period ends"}: {date(state.currentPeriodEndsAt)}</p>}
    {state.cancelAtPeriodEnd && <p>{bg ? "Абонаментът ще приключи в края на текущия период." : "Your subscription will end at the end of the current period."}</p>}
    <p>{bg ? "Eligible новите абонати получават първите 3 месеца безплатно. Крайната цена, данъците и eligibility се потвърждават от платежния доставчик." : "Eligible new subscribers receive the first 3 months free. Final price, taxes and eligibility are confirmed by the payment provider."}</p>
    {state.requiresCheckout && <button className="primary-button" disabled={busy} onClick={() => { trackEvent("subscription_checkout_started"); run(purchaseArtistSubscription, bg ? "Абонаментът е активиран." : "Subscription activated."); }}>{busy ? (bg ? "Обработване..." : "Processing...") : state.hasUsedTrial ? (bg ? "Започни абонамент" : "Start subscription") : (bg ? "Започни безплатния период" : "Start free trial")}</button>}
    {!state.requiresCheckout && <button className="secondary-button" disabled={busy} onClick={() => run(() => manageArtistSubscription(state.activeProvider), "")}>{bg ? "Управление на абонамента" : "Manage subscription"}</button>}
    {platform !== "web" && <button className="secondary-button" disabled={busy} onClick={() => run(restoreArtistSubscription, bg ? "Покупката е възстановена." : "Purchase restored.")}>{bg ? "Възстанови покупки" : "Restore purchases"}</button>}
    {message && <p className="success-message">{message}</p>}{error && <p className="error-message">{error}</p>}
  </section></main>;
}
