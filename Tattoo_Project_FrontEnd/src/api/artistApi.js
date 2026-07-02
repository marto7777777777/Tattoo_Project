import { apiRequest } from "./http";

export function getAllArtists() {
  return apiRequest("/api/TattooArtist");
}

export function getArtistById(id) {
  return apiRequest(`/api/TattooArtist/${id}`);
}

export function createArtistProfile(artistData) {
  return apiRequest("/api/TattooArtist/profile", {
    method: "POST",
    body: JSON.stringify(artistData),
  });
}

export function updateArtistProfile(artistData) {
  return apiRequest("/api/TattooArtist/profile", {
    method: "PUT",
    body: JSON.stringify(artistData),
  });
}
