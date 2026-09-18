import { Capacitor, registerPlugin } from "@capacitor/core";
import {
  getAiMobileContext,
  getMobileSubscriptionContext,
  openCustomerPortal,
  startSubscriptionCheckout,
  verifyMobileAiPurchase,
  verifyMobileSubscription,
} from "../api/subscriptionApi";
import { startAiProjectCheckout } from "../api/aiTattooApi";

const NativeBilling = registerPlugin("InkRouteBilling");
let purchaseInProgress = false;

export const billingPlatform = () => {
  const platform = Capacitor.getPlatform();
  return platform === "android" || platform === "ios" ? platform : "web";
};

const providerFor = (platform) => platform === "android" ? "google_play" : "apple";

async function exclusivePurchase(operation) {
  if (purchaseInProgress) throw new Error("A purchase is already in progress. Please wait.");
  purchaseInProgress = true;
  try { return await operation(); }
  finally { purchaseInProgress = false; }
}

function payloadFrom(result) {
  const value = result?.purchasePayload || result?.purchaseToken || result?.signedTransaction;
  if (!value) throw new Error("The store did not return a verifiable purchase. Use Restore purchases before trying again.");
  return value;
}

async function verifyAndFinish({ result, provider, productKind, productId, aiProjectId }) {
  if (result?.status === "pending") return { pending: true };
  const purchasePayload = payloadFrom(result);
  const request = { provider, productKind, productId, purchasePayload, ...(aiProjectId ? { aiProjectId } : {}) };
  const verified = productKind === "artist_subscription"
    ? await verifyMobileSubscription(request)
    : await verifyMobileAiPurchase(request);
  if (!verified?.verified) throw new Error("The purchase is waiting for server verification. Do not purchase again; use Restore purchases shortly.");
  await NativeBilling.finishTransaction({
    productKind,
    productId,
    transactionId: result.transactionId || "",
    purchaseToken: result.purchaseToken || "",
  });
  return verified;
}

export async function purchaseArtistSubscription() {
  return exclusivePurchase(async () => {
    const platform = billingPlatform();
    if (platform === "web") {
      const checkout = await startSubscriptionCheckout();
      window.location.assign(checkout.url);
      return { redirected: true };
    }
    const provider = providerFor(platform);
    const context = await getMobileSubscriptionContext(provider);
    if (!context.artistProductId) throw new Error("The artist subscription is not configured for this store.");
    const result = await NativeBilling.purchase({
      productKind: "artist_subscription",
      productId: context.artistProductId,
      trialOfferId: context.trialEligible ? context.trialOfferId || "" : "",
      basePlanId: context.basePlanId || "",
      trialEligible: Boolean(context.trialEligible),
      obfuscatedAccountId: context.obfuscatedAccountId || "",
      appAccountToken: context.appAccountToken || "",
    });
    return verifyAndFinish({ result, provider, productKind: "artist_subscription", productId: context.artistProductId });
  });
}

export async function getArtistProductDetails() {
  const platform = billingPlatform();
  if (platform === "web") return { formattedPrice: "€14.99", productType: "subscription" };
  const provider = providerFor(platform); const context = await getMobileSubscriptionContext(provider);
  return NativeBilling.getProductDetails({ productKind: "artist_subscription", productId: context.artistProductId, basePlanId: context.basePlanId || "", trialOfferId: context.trialOfferId || "", trialEligible: Boolean(context.trialEligible) });
}

export async function getAiPassProductDetails() {
  const platform = billingPlatform();
  if (platform === "web") return { formattedPrice: "€12.49", productType: "one_time" };
  const provider = providerFor(platform); const context = await getAiMobileContext(provider);
  return NativeBilling.getProductDetails({ productKind: "ai_project_pass", productId: context.aiProjectPassProductId });
}

export async function purchaseAiProjectPass(aiProjectId) {
  return exclusivePurchase(async () => {
    const platform = billingPlatform();
    if (platform === "web") {
      const checkout = await startAiProjectCheckout(aiProjectId);
      window.location.assign(checkout.url);
      return { redirected: true };
    }
    const provider = providerFor(platform);
    const context = await getAiMobileContext(provider);
    if (!context.aiProjectPassProductId) throw new Error("The AI Project Pass is not configured for this store.");
    const result = await NativeBilling.purchase({
      productKind: "ai_project_pass",
      productId: context.aiProjectPassProductId,
      obfuscatedAccountId: context.obfuscatedAccountId || "",
      appAccountToken: context.appAccountToken || "",
    });
    return verifyAndFinish({ result, provider, productKind: "ai_project_pass", productId: context.aiProjectPassProductId, aiProjectId });
  });
}

export async function restoreArtistSubscription() {
  const platform = billingPlatform();
  if (platform === "web") throw new Error("Restore purchases is available in the mobile apps.");
  const provider = providerFor(platform);
  const context = await getMobileSubscriptionContext(provider);
  const restored = await NativeBilling.restorePurchases({ productKind: "artist_subscription", productId: context.artistProductId });
  const purchases = restored?.purchases || [];
  if (!purchases.length) throw new Error("No restorable subscription was found for this store account.");
  let latest;
  for (const result of purchases) latest = await verifyAndFinish({ result, provider, productKind: "artist_subscription", productId: context.artistProductId });
  return latest;
}

export async function restoreAiProjectPass(aiProjectId) {
  const platform = billingPlatform();
  if (platform === "web") throw new Error("Restore purchases is available in the mobile apps.");
  const provider = providerFor(platform);
  const context = await getAiMobileContext(provider);
  const restored = await NativeBilling.restorePurchases({ productKind: "ai_project_pass", productId: context.aiProjectPassProductId });
  const purchases = restored?.purchases || [];
  if (!purchases.length) throw new Error("No unfinished AI purchase was found. Do not buy again if verification is still pending.");
  let latest;
  for (const result of purchases) latest = await verifyAndFinish({ result, provider, productKind: "ai_project_pass", productId: context.aiProjectPassProductId, aiProjectId });
  return latest;
}

export async function manageArtistSubscription(activeProvider) {
  const platform = billingPlatform();
  if (activeProvider === "stripe") {
    if (platform !== "web") throw new Error("This subscription was purchased on the web. Manage it at inkroute.app.");
    const portal = await openCustomerPortal();
    window.location.assign(portal.url);
    return;
  }
  if ((activeProvider === "google_play" && platform !== "android") || (activeProvider === "apple" && platform !== "ios")) {
    throw new Error(`This subscription must be managed through ${activeProvider === "apple" ? "the App Store" : "Google Play"}.`);
  }
  await NativeBilling.openSubscriptionManagement();
}
