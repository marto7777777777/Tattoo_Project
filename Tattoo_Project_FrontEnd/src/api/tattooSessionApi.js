import { apiRequest } from "./http";

export function createTattooSession(sessionData) {
  return apiRequest("/api/TattooSession", {
    method: "POST",
    body: JSON.stringify(sessionData),
  });
}

export function updateTattooSession(id, sessionData) {
  return apiRequest(`/api/TattooSession/${id}`, {
    method: "PUT",
    body: JSON.stringify(sessionData),
  });
}

export function addMoreSessions(tattooRequestId, data) {
  return apiRequest(`/api/TattooSession/add-more-sessions/${tattooRequestId}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function completeTattoo(tattooRequestId) {
  return apiRequest(`/api/TattooSession/complete-tattoo/${tattooRequestId}`, {
    method: "PUT",
  });
}
