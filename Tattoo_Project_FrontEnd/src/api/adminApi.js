import { requestJson } from "./http";

export function getAdminOverview() {
  return requestJson("/api/admin/overview");
}

export function getAdminUsers() {
  return requestJson("/api/admin/users");
}

export function getAdminTattooRequests() {
  return requestJson("/api/admin/tattoo-requests");
}

export function getAdminAiProjects() {
  return requestJson("/api/admin/ai-projects");
}

export function deleteAdminUser(userId) {
  return requestJson(`/api/admin/users/${userId}`, { method: "DELETE" });
}

export function deleteAdminClientProfile(clientId) {
  return requestJson(`/api/admin/client-profiles/${clientId}`, { method: "DELETE" });
}

export function deleteAdminArtistProfile(artistId) {
  return requestJson(`/api/admin/artist-profiles/${artistId}`, { method: "DELETE" });
}

export function deleteAdminTattooRequest(requestId) {
  return requestJson(`/api/admin/tattoo-requests/${requestId}`, { method: "DELETE" });
}

export function deleteAdminAiProject(projectId) {
  return requestJson(`/api/admin/ai-projects/${projectId}`, { method: "DELETE" });
}

export function setAdminArtistVerified(artistId, isVerified) {
  return requestJson(`/api/admin/artist-profiles/${artistId}/verified`, {
    method: "PATCH",
    body: JSON.stringify({ isVerified }),
  });
}
