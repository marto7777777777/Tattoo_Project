const META_PIXEL_ID = import.meta.env.VITE_META_DATASET_ID?.trim();

let initialized = false;

function bootstrapMetaPixel() {
  if (window.fbq) return;

  const fbq = function () {
    if (fbq.callMethod) {
      fbq.callMethod.apply(fbq, arguments);
    } else {
      fbq.queue.push(arguments);
    }
  };

  window.fbq = fbq;
  if (!window._fbq) window._fbq = fbq;

  fbq.push = fbq;
  fbq.loaded = true;
  fbq.version = "2.0";
  fbq.queue = [];

  const script = document.createElement("script");
  script.async = true;
  script.src = "https://connect.facebook.net/en_US/fbevents.js";
  script.dataset.inkrouteMetaPixel = "true";

  const firstScript = document.getElementsByTagName("script")[0];
  if (firstScript?.parentNode) {
    firstScript.parentNode.insertBefore(script, firstScript);
  } else {
    document.head.appendChild(script);
  }
}

export function enableMetaPixel() {
  if (typeof window === "undefined" || !META_PIXEL_ID) return;

  bootstrapMetaPixel();

  if (!initialized) {
    window.fbq("init", META_PIXEL_ID);
    initialized = true;
  }

  window.fbq("consent", "grant");
}

export function disableMetaPixel() {
  if (typeof window === "undefined" || !window.fbq) return;
  window.fbq("consent", "revoke");
}

export function trackMetaPageView() {
  if (typeof window === "undefined" || !window.fbq || !initialized) return;
  window.fbq("track", "PageView");
}
