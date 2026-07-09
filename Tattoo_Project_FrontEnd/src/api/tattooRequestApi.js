import { apiRequest } from "./http";

export function getAllTattooRequests() {
  return apiRequest("/api/TattooRequest");
}

export function getTattooRequestById(id) {
  return apiRequest(`/api/TattooRequest/${id}`);
}

export function getMyTattooRequests() {
  return apiRequest("/api/TattooRequest/my-requests");
}

export function getMyArtistTattooRequests() {
  return apiRequest("/api/TattooRequest/my-artist-requests");
}

export function createTattooRequest(requestData) {
  return apiRequest("/api/TattooRequest", {
    method: "POST",
    body: JSON.stringify(requestData),
  });
}

export function updateTattooRequest(id, requestData) {
  return apiRequest(`/api/TattooRequest/${id}`, {
    method: "PUT",
    body: JSON.stringify(requestData),
  });
}
