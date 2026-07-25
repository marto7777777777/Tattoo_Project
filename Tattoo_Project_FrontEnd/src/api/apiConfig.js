const configuredApiUrl = import.meta.env.VITE_API_BASE_URL?.trim();

// Local development keeps the existing backend address. Mobile and production
// builds can provide their public HTTPS API through VITE_API_BASE_URL.
export const API_BASE_URL = (
  configuredApiUrl || "https://localhost:7115"
).replace(/\/+$/, "");
