const allowedEvents = new Set(["landing_page_view", "primary_cta_click", "secondary_cta_click", "registration_click", "app_store_click", "google_play_click", "video_play", "workflow_video_play", "chapter_click", "faq_open", "scroll_depth"]);

export function trackLandingEvent(name, detail = {}) {
  if (!allowedEvents.has(name)) return;
  window.dispatchEvent(new CustomEvent("inkroute:landing", { detail: { name, ...detail } }));
  if (typeof window.inkrouteTrack === "function") window.inkrouteTrack(name, detail);
  if (name === "landing_page_view") trackEvent(name);
}

const eventAliases = { page_view: "landing_page_view", cta_click: "primary_cta_click" };

export function trackLanding(name, detail = {}) {
  trackLandingEvent(eventAliases[name] || name, detail);
}
import { trackEvent } from "../services/analyticsService";
