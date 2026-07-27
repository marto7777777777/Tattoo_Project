import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { supportedLanguages } from "./translations";
import { translateUiText } from "./translate";

const STORAGE_KEY = "inkroute-language";
const LanguageContext = createContext(null);

function detectInitialLanguage() {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (supportedLanguages.some((language) => language.code === saved)) return saved;
  } catch {
    // Private browsing can disable storage. Device detection still works.
  }

  const deviceLanguages = navigator.languages?.length
    ? navigator.languages
    : [navigator.language];

  const deviceLanguage = deviceLanguages
    .map((item) => item?.toLowerCase().split("-")[0])
    .find((item) => supportedLanguages.some((language) => language.code === item));

  return deviceLanguage || "en";
}

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(detectInitialLanguage);

  const setLanguage = (nextLanguage) => {
    if (!supportedLanguages.some((item) => item.code === nextLanguage)) return;
    setLanguageState(nextLanguage);
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
