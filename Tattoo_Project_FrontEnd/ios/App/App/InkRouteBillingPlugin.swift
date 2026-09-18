import Foundation
import Capacitor
import StoreKit
import UIKit

@objc(InkRouteBillingPlugin)
public class InkRouteBillingPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "InkRouteBillingPlugin"
    public let jsName = "InkRouteBilling"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "purchase", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "getProductDetails", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "restorePurchases", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "finishTransaction", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "openSubscriptionManagement", returnType: CAPPluginReturnPromise)
    ]

    @objc func getProductDetails(_ call: CAPPluginCall) {
        guard let productId = call.getString("productId"), !productId.isEmpty else { call.reject("Missing App Store product ID."); return }
        Task {
            do {
                guard let product = try await Product.products(for: [productId]).first else { call.reject("Product details are unavailable."); return }
                let kind: String
                switch product.type { case .autoRenewable: kind = "subscription"; case .consumable: kind = "consumable"; default: kind = "one_time" }
                call.resolve(["productId": product.id, "displayName": product.displayName, "formattedPrice": product.displayPrice, "currencyCode": product.priceFormatStyle.currencyCode, "productType": kind, "trialAvailable": product.subscription?.introductoryOffer != nil])
            } catch { call.reject("Product details are unavailable: \(error.localizedDescription)") }
        }
    }

    @objc func purchase(_ call: CAPPluginCall) {
        guard let productId = call.getString("productId"), !productId.isEmpty else { call.reject("Missing App Store product ID."); return }
        Task {
            do {
                guard let product = try await Product.products(for: [productId]).first else { call.reject("This product is not available in the App Store."); return }
                var options: Set<Product.PurchaseOption> = []
                if let rawToken = call.getString("appAccountToken"), let token = UUID(uuidString: rawToken) { options.insert(.appAccountToken(token)) }
                let result = try await product.purchase(options: options)
                switch result {
                case .success(let verification):
                    let transaction = try self.verified(verification)
                    call.resolve(self.purchaseObject(transaction: transaction, jws: verification.jwsRepresentation, kind: call.getString("productKind") ?? ""))
                case .pending: call.resolve(["status": "pending"])
                case .userCancelled: call.reject("Purchase cancelled.")
                @unknown default: call.reject("Unknown App Store purchase result.")
                }
            } catch { call.reject("App Store purchase failed: \(error.localizedDescription)") }
        }
    }

    @objc func restorePurchases(_ call: CAPPluginCall) {
        let expected = call.getString("productId") ?? ""
        let kind = call.getString("productKind") ?? ""
        Task {
            do {
                if kind == "artist_subscription" { try await AppStore.sync() }
                var values: [[String: Any]] = []
                for await verification in Transaction.unfinished {
                    if case .verified(let transaction) = verification, transaction.productID == expected {
                        values.append(self.purchaseObject(transaction: transaction, jws: verification.jwsRepresentation, kind: kind))
                    }
                }
                if kind == "artist_subscription" {
                    for await verification in Transaction.currentEntitlements {
                        if case .verified(let transaction) = verification, transaction.productID == expected, !values.contains(where: { ($0["transactionId"] as? String) == String(transaction.id) }) {
                            values.append(self.purchaseObject(transaction: transaction, jws: verification.jwsRepresentation, kind: kind))
                        }
                    }
                }
                call.resolve(["purchases": values])
            } catch { call.reject("App Store restore failed: \(error.localizedDescription)") }
        }
    }

    @objc func finishTransaction(_ call: CAPPluginCall) {
        guard let rawId = call.getString("transactionId"), let id = UInt64(rawId) else { call.reject("Missing App Store transaction ID."); return }
        Task {
            for await verification in Transaction.unfinished {
                if case .verified(let transaction) = verification, transaction.id == id { await transaction.finish(); call.resolve(); return }
            }
            for await verification in Transaction.currentEntitlements {
                if case .verified(let transaction) = verification, transaction.id == id { await transaction.finish(); call.resolve(); return }
            }
            call.resolve()
        }
    }

    @objc func openSubscriptionManagement(_ call: CAPPluginCall) {
        DispatchQueue.main.async {
            guard let url = URL(string: "https://apps.apple.com/account/subscriptions") else { call.reject("Cannot open App Store subscription management."); return }
            UIApplication.shared.open(url, options: [:]) { opened in opened ? call.resolve() : call.reject("Cannot open App Store subscription management.") }
        }
    }

    private func verified<T>(_ result: VerificationResult<T>) throws -> T {
        switch result { case .verified(let value): return value; case .unverified: throw StoreError.failedVerification }
    }
    private func purchaseObject(transaction: Transaction, jws: String, kind: String) -> [String: Any] {
        ["status": "purchased", "signedTransaction": jws, "purchasePayload": jws, "transactionId": String(transaction.id), "productKind": kind]
    }
    enum StoreError: Error { case failedVerification }
}
