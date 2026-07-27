export function getUiLanguage() {
  return document.documentElement.lang === "bg" ? "bg" : "en";
}

export function getUiLocale() {
  return getUiLanguage() === "bg" ? "bg-BG" : "en-GB";
}

