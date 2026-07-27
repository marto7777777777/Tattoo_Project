import { useLanguage } from "../i18n/LanguageContext";
import { supportedLanguages } from "../i18n/translations";

export default function LanguageSwitcher({ className = "" }) {
  const { language, toggleLanguage } = useLanguage();
  const activeLanguage = supportedLanguages.find((item) => item.code === language)
    || supportedLanguages[0];
  const nextLabel = language === "bg" ? "English" : "Български";

  return (
    <button
      type="button"
      className={`language-switcher ${className}`.trim()}
      onClick={toggleLanguage}
      aria-label={`Switch language to ${nextLabel}`}
      title={`Switch language to ${nextLabel}`}
      data-i18n-ignore
    >
      <span className="language-switcher-flag" aria-hidden="true">{activeLanguage.flag}</span>
      <span className="language-switcher-code">{activeLanguage.label}</span>
    </button>
  );
}

