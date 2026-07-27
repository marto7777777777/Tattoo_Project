import { useEffect } from "react";
import { useLanguage } from "./LanguageContext";
import { translateUiText } from "./translate";

const translatedAttributes = ["placeholder", "aria-label", "title", "alt"];
const textMetadata = new WeakMap();
const attributeMetadata = new WeakMap();

function isIgnored(node) {
  const element = node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement;
  return Boolean(element?.closest("[data-i18n-ignore]"));
}

function translateTextNode(node, language) {
  if (isIgnored(node)) return;

  const current = node.nodeValue;
  if (!current?.trim()) return;

  let metadata = textMetadata.get(node);
  if (!metadata) {
    metadata = { original: current, rendered: current };
    textMetadata.set(node, metadata);
  } else if (current !== metadata.rendered && current !== metadata.original) {
    metadata.original = current;
  }

  const leading = metadata.original.match(/^\s*/)?.[0] || "";
  const trailing = metadata.original.match(/\s*$/)?.[0] || "";
  const originalText = metadata.original.trim();
  const translated = language === "en"
    ? originalText
    : translateUiText(originalText, language);
  const nextValue = `${leading}${translated}${trailing}`;

  metadata.rendered = nextValue;
  if (current !== nextValue) node.nodeValue = nextValue;
}

function translateAttribute(element, attribute, language) {
  if (isIgnored(element) || !element.hasAttribute(attribute)) return;

  let elementMetadata = attributeMetadata.get(element);
  if (!elementMetadata) {
    elementMetadata = {};
    attributeMetadata.set(element, elementMetadata);
  }

  const current = element.getAttribute(attribute);
  let metadata = elementMetadata[attribute];
  if (!metadata) {
    metadata = { original: current, rendered: current };
    elementMetadata[attribute] = metadata;
  } else if (current !== metadata.rendered && current !== metadata.original) {
    metadata.original = current;
  }

  const nextValue = language === "en"
    ? metadata.original
    : translateUiText(metadata.original, language);

  metadata.rendered = nextValue;
  if (current !== nextValue) element.setAttribute(attribute, nextValue);
}

function translateTree(root, language) {
  if (root.nodeType === Node.TEXT_NODE) {
    translateTextNode(root, language);
    return;
  }

  if (root.nodeType !== Node.ELEMENT_NODE || isIgnored(root)) return;

  translatedAttributes.forEach((attribute) => translateAttribute(root, attribute, language));

  const walker = document.createTreeWalker(
    root,
    NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT,
  );

  let node = walker.nextNode();
  while (node) {
    if (node.nodeType === Node.TEXT_NODE) {
      translateTextNode(node, language);
    } else {
      translatedAttributes.forEach((attribute) => translateAttribute(node, attribute, language));
    }
    node = walker.nextNode();
  }
}

export default function DomTranslator() {
  const { language } = useLanguage();

  useEffect(() => {
    const root = document.getElementById("root");
    if (!root) return undefined;

    translateTree(root, language);

    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === "characterData") {
          translateTextNode(mutation.target, language);
          return;
        }

        if (mutation.type === "attributes") {
          translateAttribute(mutation.target, mutation.attributeName, language);
          return;
        }

        mutation.addedNodes.forEach((node) => translateTree(node, language));
      });
    });

    observer.observe(root, {
      subtree: true,
      childList: true,
      characterData: true,
      attributes: true,
      attributeFilter: translatedAttributes,
    });

    return () => observer.disconnect();
  }, [language]);

  return null;
}

