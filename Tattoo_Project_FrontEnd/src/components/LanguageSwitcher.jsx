import { useEffect, useId, useRef, useState } from "react";
import { useLanguage } from "../i18n/LanguageContext";
import { supportedLanguages } from "../i18n/translations";

export default function LanguageSwitcher({ className = "" }) {
  const { language, setLanguage } = useLanguage();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef(null);
  const menuId = useId();
  const activeLanguage = supportedLanguages.find((item) => item.code === language)
    || supportedLanguages[0];

  useEffect(() => {
    if (!isOpen) return undefined;

    function closeOnOutsidePress(event) {
      if (!containerRef.current?.contains(event.target)) setIsOpen(false);
    }

    function closeOnEscape(event) {
      if (event.key === "Escape") setIsOpen(false);
    }

    document.addEventListener("pointerdown", closeOnOutsidePress);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOnOutsidePress);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [isOpen]);

  return (
    <div
      ref={containerRef}
      className={`language-switcher-wrap ${className}`.trim()}
      data-i18n-ignore
    >
      <button
        type="button"
        className="language-switcher"
        onClick={() => setIsOpen((current) => !current)}
        aria-label="Choose language"
        title="Choose language"
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={menuId}
      >
        <span className="language-switcher-flag" aria-hidden="true">{activeLanguage.flag}</span>
        <span className="language-switcher-code">{activeLanguage.label}</span>
        <span className={`language-switcher-chevron ${isOpen ? "language-switcher-chevron-open" : ""}`} aria-hidden="true">⌄</span>
      </button>

      {isOpen && (
        <div className="language-menu" id={menuId} role="listbox" aria-label="Language">
          {supportedLanguages.map((item) => (
            <button
              type="button"
              role="option"
              aria-selected={item.code === language}
              className={`language-menu-option ${item.code === language ? "language-menu-option-active" : ""}`}
              key={item.code}
              onClick={() => {
                setLanguage(item.code);
                setIsOpen(false);
              }}
            >
              <span aria-hidden="true">{item.flag}</span>
              <span>{item.nativeName}</span>
              <strong>{item.label}</strong>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
