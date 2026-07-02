import { apiRequest } from "./http";

export function getMyArtistResponses() {
  return apiRequest("/api/ArtistResponse/my-responses");
}

export function createArtistResponse(responseData) {
  return apiRequest("/api/ArtistResponse", {
    method: "POST",
    body: JSON.stringify(responseData),
  });
}

export function rejectTattooRequest(tattooRequestId) {
  return apiRequest(`/api/ArtistResponse/reject-tattoo-request/${tattooRequestId}`, {
    method: "PUT",
  });
}
