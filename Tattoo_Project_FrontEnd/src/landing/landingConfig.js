export const landingVideos = {
  overview: { id: "overview", titleKey: "videoOverview", descriptionKey: "videoOverviewDescription", src: "", poster: "/landing/media/overview-poster.webp", chapters: ["chapterWhat", "chapterProfile", "chapterLink", "chapterRequest", "chapterResponse", "chapterConsultation", "chapterPlan", "chapterSchedule", "chapterBooking", "chapterComplete"], tracks: [] },
  featured: {
    artistSetup: { id: "artist-setup-and-schedule", src: "https://www.youtube.com/watch?v=ayKkQlYRhA4", poster: "/landing/media/artist-setup-and-schedule-poster.webp", tracks: [] },
    completeWorkflow: { id: "complete-workflow", src: "https://www.youtube.com/watch?v=s3p5396zidg", poster: "/landing/media/complete-workflow-poster.webp", tracks: [] },
  },
  topics: [
    ["artist-profile", "videoProfile"], ["public-link", "videoLink"], ["structured-request", "videoRequest"],
    ["artist-response", "videoResponse"], ["book-consultation", "videoConsultation"], ["complete-consultation", "videoCompleteConsultation"],
    ["book-sessions", "videoSessions"], ["extra-sessions", "videoExtraSessions"], ["weekly-schedule", "videoWeeklySchedule"],
    ["time-off", "videoTimeOff"], ["portfolio-requirements", "videoPortfolio"], ["project-completion", "videoCompletion"],
  ].map(([id, titleKey]) => ({ id, titleKey, descriptionKey: "videoComingSoon", src: "", poster: `/landing/media/${id}-poster.webp`, tracks: [] })),
};

export const landingEnvironment = {
  appStoreUrl: import.meta.env.VITE_APP_STORE_URL?.trim() || "",
  googlePlayUrl: import.meta.env.VITE_GOOGLE_PLAY_URL?.trim() || "",
  webRegisterUrl: import.meta.env.VITE_WEB_REGISTER_URL?.trim() || "/register",
  canonicalUrl: import.meta.env.VITE_LANDING_CANONICAL_URL?.trim() || "",
  ogImageUrl: import.meta.env.VITE_LANDING_OG_IMAGE_URL?.trim() || "",
};

export function getDeviceCta() {
  const agent = navigator.userAgent || "";
  const isApple = /iPhone|iPad|iPod/i.test(agent);
  const isAndroid = /Android/i.test(agent);
  if (isApple && landingEnvironment.appStoreUrl) return { href: landingEnvironment.appStoreUrl, store: "app-store" };
  if (isAndroid && landingEnvironment.googlePlayUrl) return { href: landingEnvironment.googlePlayUrl, store: "google-play" };
  return { href: landingEnvironment.webRegisterUrl || "/register", store: "web" };
}
