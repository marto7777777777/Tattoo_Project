import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { readConsent } from "../services/consentService";
import {
  disableMetaPixel,
  enableMetaPixel,
  trackMetaPageView,
} from "../services/metaPixelService";

export default function MetaPixelSync() {
  const location = useLocation();
  const [marketingConsent, setMarketingConsent] = useState(
    () => readConsent().marketing,
  );

  useEffect(() => {
    const handleConsentChange = (event) => {
      setMarketingConsent(Boolean(event.detail?.marketing));
    };

    window.addEventListener("inkroute:consent-changed", handleConsentChange);
    return () =>
      window.removeEventListener("inkroute:consent-changed", handleConsentChange);
  }, []);

  useEffect(() => {
    if (!marketingConsent) {
      disableMetaPixel();
      return;
    }

    enableMetaPixel();
    trackMetaPageView();
  }, [marketingConsent, location.pathname, location.search]);

  return null;
}
