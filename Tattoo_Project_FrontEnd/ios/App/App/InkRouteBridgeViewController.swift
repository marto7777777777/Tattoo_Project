import Capacitor

@objc(InkRouteBridgeViewController)
class InkRouteBridgeViewController: CAPBridgeViewController {
    override func capacitorDidLoad() {
        bridge?.registerPluginInstance(InkRouteBillingPlugin())
    }
}
