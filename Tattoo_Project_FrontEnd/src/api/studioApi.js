import { requestJson } from "./http";

export function getStudios(query = "") {
  const suffix = query.trim() ? `?query=${encodeURIComponent(query.trim())}` : "";
  return requestJson(`/api/Studio${suffix}`);
}

export function getStudioById(studioId) {
  return requestJson(`/api/Studio/${studioId}`);
}

export function searchOpenStudiosForJoin(query) {
  return requestJson(`/api/Studio/join-search?query=${encodeURIComponent(query.trim())}`);
}

export function getMyStudio() {
  return requestJson("/api/Studio/mine");
}

export function acceptStudioJoinRequest(requestId) {
  return requestJson(`/api/Studio/join-requests/${requestId}/accept`, { method: "POST" });
}

export function rejectStudioJoinRequest(requestId) {
  return requestJson(`/api/Studio/join-requests/${requestId}/reject`, { method: "POST" });
}

export function removeStudioMember(artistId) {
  return requestJson(`/api/Studio/members/${artistId}`, { method: "DELETE" });
}

export function setStudioOpenForJoinRequests(isOpen) {
  return requestJson("/api/Studio/open-for-join-requests", {
    method: "PATCH",
    body: JSON.stringify({ isOpen }),
  });
}

export function updateMyStudio(studio) {
  return requestJson("/api/Studio/mine", {
    method: "PUT",
    body: JSON.stringify(studio),
  });
}

export function requestJoinStudio(studioId) {
  return requestJson(`/api/Studio/${studioId}/join`, { method: "POST" });
}

export function createMyStudio(studio) {
  return requestJson("/api/Studio/mine/create", {
    method: "POST",
    body: JSON.stringify({ ...studio, latitude: null, longitude: null }),
  });
}
