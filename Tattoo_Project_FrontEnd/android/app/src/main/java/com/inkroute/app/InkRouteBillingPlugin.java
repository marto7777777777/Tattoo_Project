package com.inkroute.app;

import android.content.Intent;
import android.net.Uri;
import com.android.billingclient.api.*;
import com.getcapacitor.JSArray;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;
import java.util.*;

@CapacitorPlugin(name = "InkRouteBilling")
public class InkRouteBillingPlugin extends Plugin implements PurchasesUpdatedListener {
    private BillingClient billingClient;
    private PluginCall activeCall;
    private String activeKind;

    @Override public void load() {
        billingClient = BillingClient.newBuilder(getContext()).setListener(this)
            .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
            .enableAutoServiceReconnection().build();
        billingClient.startConnection(new BillingClientStateListener() {
            public void onBillingSetupFinished(BillingResult result) {}
            public void onBillingServiceDisconnected() {}
        });
    }
    private void ready(Runnable action, PluginCall call) {
        if (billingClient.isReady()) { action.run(); return; }
        billingClient.startConnection(new BillingClientStateListener() {
            public void onBillingSetupFinished(BillingResult result) { if (result.getResponseCode() == BillingClient.BillingResponseCode.OK) action.run(); else call.reject("Google Play Billing is unavailable: " + result.getDebugMessage()); }
            public void onBillingServiceDisconnected() { call.reject("Google Play Billing disconnected."); }
        });
    }
    @PluginMethod public void purchase(PluginCall call) {
        if (activeCall != null) { call.reject("A purchase is already in progress."); return; }
        String productId = call.getString("productId", ""), kind = call.getString("productKind", "");
        String type = "artist_subscription".equals(kind) ? BillingClient.ProductType.SUBS : BillingClient.ProductType.INAPP;
        ready(() -> {
            QueryProductDetailsParams.Product product = QueryProductDetailsParams.Product.newBuilder().setProductId(productId).setProductType(type).build();
            billingClient.queryProductDetailsAsync(QueryProductDetailsParams.newBuilder().setProductList(Collections.singletonList(product)).build(), (result, detailsResult) -> {
                if (result.getResponseCode() != BillingClient.BillingResponseCode.OK || detailsResult.getProductDetailsList().isEmpty()) { call.reject("This product is not available in Google Play."); return; }
                ProductDetails details = detailsResult.getProductDetailsList().get(0);
                BillingFlowParams.ProductDetailsParams.Builder item = BillingFlowParams.ProductDetailsParams.newBuilder().setProductDetails(details);
                if (BillingClient.ProductType.SUBS.equals(type)) {
                    String wanted = call.getString("trialOfferId", "");
                    String basePlanId = call.getString("basePlanId", "");
                    boolean trialEligible = call.getBoolean("trialEligible", false);
                    List<ProductDetails.SubscriptionOfferDetails> offers = details.getSubscriptionOfferDetails();
                    ProductDetails.SubscriptionOfferDetails selected = null;
                    if (offers != null) for (ProductDetails.SubscriptionOfferDetails offer : offers) {
                        boolean baseMatches = !basePlanId.isEmpty() && basePlanId.equals(offer.getBasePlanId());
                        boolean offerMatches = trialEligible ? !wanted.isEmpty() && wanted.equals(offer.getOfferId()) : offer.getOfferId() == null;
                        if (baseMatches && offerMatches) { selected = offer; break; }
                    }
                    if (selected == null) { call.reject(trialEligible ? "The eligible trial offer is not available for this Google Play account. Refresh and try again." : "The configured paid Google Play base plan is not available."); return; }
                    item.setOfferToken(selected.getOfferToken());
                }
                BillingFlowParams.Builder flow = BillingFlowParams.newBuilder().setProductDetailsParamsList(Collections.singletonList(item.build()));
                String account = call.getString("obfuscatedAccountId", ""); if (!account.isEmpty()) flow.setObfuscatedAccountId(account);
                activeCall = call; activeKind = kind;
                BillingResult launched = billingClient.launchBillingFlow(getActivity(), flow.build());
                if (launched.getResponseCode() != BillingClient.BillingResponseCode.OK) { activeCall = null; activeKind = null; call.reject("Google Play could not start the purchase: " + launched.getDebugMessage()); }
            });
        }, call);
    }
    @PluginMethod public void getProductDetails(PluginCall call) {
        String productId = call.getString("productId", ""), kind = call.getString("productKind", "");
        String type = "artist_subscription".equals(kind) ? BillingClient.ProductType.SUBS : BillingClient.ProductType.INAPP;
        ready(() -> {
            QueryProductDetailsParams.Product product = QueryProductDetailsParams.Product.newBuilder().setProductId(productId).setProductType(type).build();
            billingClient.queryProductDetailsAsync(QueryProductDetailsParams.newBuilder().setProductList(Collections.singletonList(product)).build(), (result, detailsResult) -> {
                if (result.getResponseCode() != BillingClient.BillingResponseCode.OK || detailsResult.getProductDetailsList().isEmpty()) { call.reject("Product details are unavailable."); return; }
                ProductDetails details = detailsResult.getProductDetailsList().get(0); JSObject out = new JSObject();
                out.put("productId", details.getProductId()); out.put("displayName", details.getName()); out.put("productType", type);
                if (BillingClient.ProductType.INAPP.equals(type) && details.getOneTimePurchaseOfferDetails() != null) {
                    out.put("formattedPrice", details.getOneTimePurchaseOfferDetails().getFormattedPrice()); out.put("currencyCode", details.getOneTimePurchaseOfferDetails().getPriceCurrencyCode());
                } else {
                    String basePlanId = call.getString("basePlanId", ""), trialOfferId = call.getString("trialOfferId", ""); boolean trialEligible = call.getBoolean("trialEligible", false);
                    ProductDetails.SubscriptionOfferDetails selected = null; List<ProductDetails.SubscriptionOfferDetails> offers = details.getSubscriptionOfferDetails();
                    if (offers != null) for (ProductDetails.SubscriptionOfferDetails offer : offers) if (basePlanId.equals(offer.getBasePlanId()) && (trialEligible ? trialOfferId.equals(offer.getOfferId()) : offer.getOfferId() == null)) { selected = offer; break; }
                    if (selected == null) { call.reject("Configured subscription offer is unavailable."); return; }
                    List<ProductDetails.PricingPhase> phases = selected.getPricingPhases().getPricingPhaseList(); ProductDetails.PricingPhase paid = phases.get(phases.size()-1);
                    out.put("formattedPrice", paid.getFormattedPrice()); out.put("currencyCode", paid.getPriceCurrencyCode()); out.put("basePlanId", selected.getBasePlanId()); out.put("trialAvailable", phases.size() > 1 || paid.getPriceAmountMicros() == 0);
                }
                call.resolve(out);
            });
        }, call);
    }
    @Override public void onPurchasesUpdated(BillingResult result, List<Purchase> purchases) {
        PluginCall call = activeCall; activeCall = null;
        if (call == null) return;
        if (result.getResponseCode() == BillingClient.BillingResponseCode.USER_CANCELED) { call.reject("Purchase cancelled."); return; }
        if (result.getResponseCode() != BillingClient.BillingResponseCode.OK && result.getResponseCode() != BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED) { call.reject("Google Play purchase failed: " + result.getDebugMessage()); return; }
        if (purchases == null || purchases.isEmpty()) { call.reject("Purchase already exists. Use Restore purchases."); return; }
        call.resolve(purchaseObject(purchases.get(0), activeKind)); activeKind = null;
    }
    private JSObject purchaseObject(Purchase purchase, String kind) {
        JSObject out = new JSObject(); out.put("purchaseToken", purchase.getPurchaseToken()); out.put("purchasePayload", purchase.getPurchaseToken());
        out.put("transactionId", purchase.getOrderId() == null ? "" : purchase.getOrderId()); out.put("productKind", kind == null ? "" : kind);
        out.put("status", purchase.getPurchaseState() == Purchase.PurchaseState.PENDING ? "pending" : "purchased"); return out;
    }
    @PluginMethod public void restorePurchases(PluginCall call) {
        String kind = call.getString("productKind", ""), expected = call.getString("productId", "");
        String type = "artist_subscription".equals(kind) ? BillingClient.ProductType.SUBS : BillingClient.ProductType.INAPP;
        ready(() -> billingClient.queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(type).build(), (result, purchases) -> {
            if (result.getResponseCode() != BillingClient.BillingResponseCode.OK) { call.reject("Google Play restore failed: " + result.getDebugMessage()); return; }
            JSArray array = new JSArray(); for (Purchase purchase : purchases) if (purchase.getProducts().contains(expected)) array.put(purchaseObject(purchase, kind));
            JSObject response = new JSObject(); response.put("purchases", array); call.resolve(response);
        }), call);
    }
    @PluginMethod public void finishTransaction(PluginCall call) {
        // The backend acknowledges subscriptions and consumes AI products only after
        // authoritative server-side verification. Never duplicate that operation here.
        call.resolve();
    }
    @PluginMethod public void openSubscriptionManagement(PluginCall call) {
        Intent intent = new Intent(Intent.ACTION_VIEW, Uri.parse("https://play.google.com/store/account/subscriptions?package=" + getContext().getPackageName())); intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK); getContext().startActivity(intent); call.resolve();
    }
}
