import { API_BASE_URL } from "./apiConfig";
import { apiRequest, getToken } from "./http";

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

export function getBookingAvailability(id, bookingType) {
  return apiRequest(`/api/TattooRequest/${id}/availability?bookingType=${encodeURIComponent(bookingType)}`);
}

export function createTattooRequest(requestData) {
  return apiRequest("/api/TattooRequest", {
    method: "POST",
    body: JSON.stringify(requestData),
  });
}

export async function createTattooRequestWithImages(requestData, files = []) {
  const token = getToken();
  const formData = new FormData();

  formData.append("tattooArtistId", requestData.tattooArtistId);
  formData.append("description", requestData.description);
  formData.append("placement", requestData.placement);
  formData.append("tattooStyle", requestData.tattooStyle);

  files.forEach((file) => {
    formData.append("images", file);
  });

  return fetch(`${API_BASE_URL}/api/TattooRequest/with-images`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });
}

export function updateTattooRequest(id, requestData) {
  return apiRequest(`/api/TattooRequest/${id}`, {
    method: "PUT",
    body: JSON.stringify(requestData),
  });
}
