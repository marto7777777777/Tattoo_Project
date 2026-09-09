import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { supportedLanguages } from "./translations";
import { translateUiText } from "./translate";

const STORAGE_KEY = "inkroute-language";
const LanguageContext = createContext(null);

function isSupportedLanguage(code) {
  return supportedLanguages.some((language) => language.code === code);
}

function getUrlLanguage() {
  if (typeof window === "undefined") return null;

  const requestedLanguage = new URLSearchParams(window.location.search)
    .get("lang")
    ?.trim()
    .toLowerCase()
    .split("-")[0];

  return isSupportedLanguage(requestedLanguage) ? requestedLanguage : null;
}

function detectInitialLanguage() {
  const urlLanguage = getUrlLanguage();
  if (urlLanguage) return urlLanguage;

  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (isSupportedLanguage(saved)) return saved;
  } catch {
    // Private browsing can disable storage. Device detection still works.
  }

  const deviceLanguages = navigator.languages?.length
    ? navigator.languages
    : [navigator.language];

  const deviceLanguage = deviceLanguages
    .map((item) => item?.toLowerCase().split("-")[0])
    .find((item) => isSupportedLanguage(item));

  return deviceLanguage || "en";
}

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(detectInitialLanguage);

  const setLanguage = (nextLanguage) => {
    if (!isSupportedLanguage(nextLanguage)) return;
    setLanguageState(nextLanguage);

    if (typeof window !== "undefined" && window.location.pathname.replace(/\/+$/, "") === "/for-artists") {
      const url = new URL(window.location.href);
      url.searchParams.set("lang", nextLanguage);
      window.history.replaceState(window.history.state, "", `${url.pathname}${url.search}${url.hash}`);
    }
  };

  useEffect(() => {
    document.documentElement.lang = language;
    try {
      localStorage.setItem(STORAGE_KEY, language);
    } catch {
      // Language still remains active for the current session.
    }
  }, [language]);

  const value = useMemo(() => {
    const definition = supportedLanguages.find((item) => item.code === language);
    return {
      language,
      locale: definition?.locale || "en-GB",
      setLanguage,
      t: (text) => translateUiText(text, language),
    };
  }, [language]);

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error("useLanguage must be used inside LanguageProvider.");
  return context;
}
