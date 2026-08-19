import { useEffect } from "react";
import { landingEnvironment } from "./landingConfig";

const metadata = {
  title: "InkRoute for Tattoo Artists | From First Request to Finished Tattoo",
  description: "Organize client requests, consultations, availability and tattoo sessions in one clear process built for tattoo artists.",
};

function setMeta(selector, attributes) {
  let element = document.head.querySelector(selector);
  if (!element) { element = document.createElement("meta"); document.head.appendChild(element); }
  Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
}

export function useLandingSeo() {
  useEffect(() => {
    const previousTitle = document.title;
    document.title = metadata.title;
    setMeta('meta[name="description"]', { name: "description", content: metadata.description });
    setMeta('meta[property="og:title"]', { property: "og:title", content: metadata.title });
    setMeta('meta[property="og:description"]', { property: "og:description", content: metadata.description });
    setMeta('meta[property="og:type"]', { property: "og:type", content: "website" });
    setMeta('meta[name="twitter:card"]', { name: "twitter:card", content: "summary_large_image" });
    setMeta('meta[name="twitter:title"]', { name: "twitter:title", content: metadata.title });
    setMeta('meta[name="twitter:description"]', { name: "twitter:description", content: metadata.description });
    if (landingEnvironment.ogImageUrl) setMeta('meta[property="og:image"]', { property: "og:image", content: landingEnvironment.ogImageUrl });
    let canonical;
    if (landingEnvironment.canonicalUrl) {
      canonical = document.head.querySelector('link[rel="canonical"]') || document.createElement("link");
      canonical.rel = "canonical"; canonical.href = landingEnvironment.canonicalUrl;
      if (!canonical.parentNode) document.head.appendChild(canonical);
    }
    return () => { document.title = previousTitle; canonical?.remove(); };
  }, []);
}
