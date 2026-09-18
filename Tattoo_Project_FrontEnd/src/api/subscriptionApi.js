import { requestJson } from "./http";
export const getSubscriptionStatus=()=>requestJson("/api/subscription");
export const startSubscriptionCheckout=()=>requestJson("/api/subscription/checkout",{method:"POST"});
export const openCustomerPortal=()=>requestJson("/api/subscription/portal",{method:"POST"});
export const getMobileSubscriptionContext=(provider)=>requestJson(`/api/subscription/mobile-context?provider=${encodeURIComponent(provider)}`);
export const verifyMobileSubscription=(payload)=>requestJson("/api/subscription/mobile/verify",{method:"POST",body:JSON.stringify(payload)});
export const getAiMobileContext=(provider)=>requestJson(`/api/mobile-billing/ai-context?provider=${encodeURIComponent(provider)}`);
export const verifyMobileAiPurchase=(payload)=>requestJson("/api/mobile-billing/verify",{method:"POST",body:JSON.stringify(payload)});
