import { apiRequest, readResponse, requestJson } from "./http";
import { getSearchAliases } from "../utils/searchAliases";
import { optimizeImageForUpload } from "../utils/images";

async function searchStudiosWithAliases(path, query) {
  const aliases = getSearchAliases(query);
  let firstError = null;

  // The previous implementation fired every language alias at once. On a
  // single-core App Service that multiplied identical database work. Try the
  // user's exact query first and only fall back when it returns no matches.
  for (const alias of aliases) {
    try {
      const suffix = alias ? `?query=${encodeURIComponent(alias)}` : "";
      const result = await requestJson(`${path}${suffix}`, { cacheTtlMs: 30_000 });
      if (Array.isArray(result) && result.length > 0) return result;
      if (aliases.length === 1) return Array.isArray(result) ? result : [];
    } catch (error) {
      firstError ||= error;
    }
  }

  if (firstError) throw firstError;
  return [];
}

export function getStudios(query = "") {
  return searchStudiosWithAliases("/api/Studio", query);
}

export function getStudioById(studioId) {
  return requestJson(`/api/Studio/${studioId}`, { cacheTtlMs: 30_000 });
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

export function cancelStudioJoinRequest(requestId) {
  return requestJson(`/api/Studio/join-requests/${requestId}/cancel`, { method: "POST" });
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

export async function uploadStudioImage(kind, file) {
  const formData = new FormData();
  const isLogo = kind === "logo";
  const optimizedFile = await optimizeImageForUpload(file, {
    maxWidth: isLogo ? 1200 : 2048,
    maxHeight: isLogo ? 1200 : 2048,
    quality: 0.86,
  });
  formData.append("image", optimizedFile);
  const response = await apiRequest(`/api/Studio/mine/${kind}`, { method: "POST", body: formData });
  const data = await readResponse(response);
  if (!response.ok) throw new Error(typeof data === "string" ? data : data?.message || "Could not upload studio image.");
  return data;
}
