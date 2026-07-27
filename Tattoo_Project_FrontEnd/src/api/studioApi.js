import { requestJson } from "./http";
import { getSearchAliases } from "../utils/searchAliases";

async function searchStudiosWithAliases(path, query) {
  const aliases = getSearchAliases(query);
  const results = await Promise.allSettled(
    aliases.map((alias) => {
      const suffix = alias ? `?query=${encodeURIComponent(alias)}` : "";
      return requestJson(`${path}${suffix}`);
    }),
  );

  const successfulResults = results
    .filter((result) => result.status === "fulfilled")
    .flatMap((result) => Array.isArray(result.value) ? result.value : []);

  if (!successfulResults.length && results.every((result) => result.status === "rejected")) {
    throw results[0].reason;
  }

  return Array.from(
    new Map(successfulResults.map((studio) => [Number(studio.id), studio])).values(),
  );
}

export function getStudios(query = "") {
  return searchStudiosWithAliases("/api/Studio", query);
}

export function getStudioById(studioId) {
  return requestJson(`/api/Studio/${studioId}`);
}

export function searchOpenStudiosForJoin(query) {
  return searchStudiosWithAliases("/api/Studio/join-search", query);
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
