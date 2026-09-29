import { apiRequest } from "./http";
import { optimizeImagesForUpload } from "../utils/images";

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

export function getBookingAvailability(id, bookingType, startDate, days) {
  const query = new URLSearchParams({
    bookingType,
    startDate,
    days: String(days),
  });
  return apiRequest(`/api/TattooRequest/${id}/availability?${query}`);
}

export function createTattooRequest(requestData) {
  return apiRequest("/api/TattooRequest", {
    method: "POST",
    body: JSON.stringify(requestData),
  });
}

export async function createTattooRequestWithImages(requestData, files = []) {
  const formData = new FormData();

  formData.append("tattooArtistId", requestData.tattooArtistId);
  formData.append("description", requestData.description);
  formData.append("placement", requestData.placement);
  formData.append("tattooStyle", requestData.tattooStyle);

  const optimizedFiles = await optimizeImagesForUpload(files, {
    maxWidth: 2048,
    maxHeight: 2048,
    quality: 0.86,
  });

  optimizedFiles.forEach((file) => {
    formData.append("images", file);
  });

  return apiRequest("/api/TattooRequest/with-images", {
    method: "POST",
    body: formData,
  });
}

export function updateTattooRequest(id, requestData) {
  return apiRequest(`/api/TattooRequest/${id}`, {
    method: "PUT",
    body: JSON.stringify(requestData),
  });
}

export function rejectTattooRequestByArtist(id) {
  return apiRequest(`/api/TattooRequest/${id}/reject-by-artist`, {
    method: "PUT",
  });
}

export function markTattooRequestUnderReview(id) {
  return apiRequest(`/api/TattooRequest/${id}/under-review`, { method: "POST" });
}

export function cancelTattooRequest(id, reason = "") {
  return apiRequest(`/api/TattooRequest/${id}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reason: reason.trim() || null }),
  });
}
