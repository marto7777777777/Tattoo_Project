export function getUiLanguage() {
  const language = document.documentElement.lang;
  return ["bg", "de", "fr", "es", "it"].includes(language) ? language : "en";
}

export function getUiLocale() {
  const locales = {
    bg: "bg-BG",
    de: "de-DE",
    fr: "fr-FR",
    es: "es-ES",
    it: "it-IT",
    en: "en-GB",
  };
  return locales[getUiLanguage()] || locales.en;
}
