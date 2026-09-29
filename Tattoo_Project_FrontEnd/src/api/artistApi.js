import { apiRequest, requestJson } from "./http";

export function getPublicArtist(slug) {
  return requestJson(`/api/TattooArtist/public/${encodeURIComponent(slug)}`, { cacheTtlMs: 30_000 });
}

export async function createArtistProfile(artistData) {
  const response = await apiRequest("/api/TattooArtist/profile", {
    method: "POST",
    body: JSON.stringify(artistData),
  });

  return response;
}

export async function getAllArtists() {
  return apiRequest("/api/TattooArtist");
}

export async function searchArtists(query) {
  return apiRequest(`/api/TattooArtist/search?query=${encodeURIComponent(query)}`);
}

export async function getRecommendedArtists() {
  return apiRequest("/api/TattooArtist/recommended");
}

export async function updateArtistProfile(artistData) {
  return apiRequest("/api/TattooArtist/profile", {
    method: "PUT",
    body: JSON.stringify(artistData),
  });
}
