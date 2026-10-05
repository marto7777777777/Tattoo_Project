import { apiRequest, requestJson, readResponse } from "./http";
import { optimizeImageForUpload } from "../utils/images";

export function reportArtist(artistId, reason, description) {
  return requestJson(`/api/moderation/artists/${artistId}/reports`, {
    method: "POST",
    body: JSON.stringify({ reason, description: description || null }),
  });
}

export function getMyModerationStatus() {
  return requestJson("/api/moderation/artist/status");
}

export async function addReinstatementImage(file) {
  const formData = new FormData();
  const optimizedFile = await optimizeImageForUpload(file, { maxWidth: 2048, maxHeight: 2048, quality: 0.86 });
  formData.append("image", optimizedFile);
  const response = await apiRequest("/api/moderation/artist/reinstatement/images", { method: "POST", body: formData });
  if (!response.ok) throw new Error(await response.text());
  return readResponse(response);
}

export function deleteReinstatementImage(imageId) {
  return requestJson(`/api/moderation/artist/reinstatement/images/${imageId}`, { method: "DELETE" });
}

export function submitReinstatement() {
  return requestJson("/api/moderation/artist/reinstatement/submit", { method: "POST" });
}

export function getAdminReports() {
  return requestJson("/api/moderation/admin/reports");
}

export function getAdminModerationSubmissions() {
  return requestJson("/api/moderation/admin/submissions");
}

export function reviewAdminReport(reportId, blockArtist, decisionNote) {
  return requestJson(`/api/moderation/admin/reports/${reportId}/review`, {
    method: "POST",
    body: JSON.stringify({ blockArtist, decisionNote: decisionNote || null }),
  });
}

export function reviewAdminSubmission(submissionId, approve, adminNote) {
  return requestJson(`/api/moderation/admin/submissions/${submissionId}/review`, {
    method: "POST",
    body: JSON.stringify({ approve, adminNote: adminNote || null }),
  });
}
