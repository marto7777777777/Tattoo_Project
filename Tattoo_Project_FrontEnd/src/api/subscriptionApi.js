import { requestJson } from "./http";
export const getSubscriptionStatus=()=>requestJson("/api/subscription");
export const startSubscriptionCheckout=()=>requestJson("/api/subscription/checkout",{method:"POST"});
export const openCustomerPortal=()=>requestJson("/api/subscription/portal",{method:"POST"});
