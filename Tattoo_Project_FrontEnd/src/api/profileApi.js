import { apiRequest, requestJson, readResponse } from "./http";
import { optimizeImageForUpload } from "../utils/images";

export function getMyProfile() {
  return requestJson("/api/Profile/me");
}

export function updateStringField(path, value) {
  return requestJson(path, {
    method: "PATCH",
    body: JSON.stringify({ value }),
  });
}

export function updateNumberField(path, value) {
  return requestJson(path, {
    method: "PATCH",
    body: JSON.stringify({ value: value === "" ? null : Number(value) }),
  });
}

export function updateBoolField(path, value) {
  return requestJson(path, {
    method: "PATCH",
    body: JSON.stringify({ value: Boolean(value) }),
  });
}

export function updateSpecialtyStyles(values) {
  return requestJson("/api/Profile/artist/specialty-styles", {
    method: "PATCH",
    body: JSON.stringify({ values }),
  });
}

export async function updateProfileImage(file) {
  const formData = new FormData();
  const optimizedFile = await optimizeImageForUpload(file, {
    maxWidth: 1200,
    maxHeight: 1200,
    quality: 0.86,
  });
  formData.append("image", optimizedFile);

  const response = await apiRequest("/api/Profile/contact/profile-image", {
    method: "PATCH",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return readResponse(response);
}

export function addRequirement(description) {
  return requestJson("/api/Profile/studio/requirements", {
    method: "POST",
    body: JSON.stringify({ value: description }),
  });
}

export function updateRequirement(id, description) {
  return requestJson(`/api/Profile/studio/requirements/${id}`, {
    method: "PATCH",
    body: JSON.stringify({ value: description }),
  });
}

export function deleteRequirement(id) {
  return requestJson(`/api/Profile/studio/requirements/${id}`, {
    method: "DELETE",
  });
}

export async function addPortfolioImage(file) {
  const formData = new FormData();
  const optimizedFile = await optimizeImageForUpload(file, {
    maxWidth: 2048,
    maxHeight: 2048,
    quality: 0.86,
  });
  formData.append("image", optimizedFile);

  const response = await apiRequest("/api/Profile/portfolio/images", {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return readResponse(response);
}

export function deletePortfolioImage(id) {
  return requestJson(`/api/Profile/portfolio/images/${id}`, {
    method: "DELETE",
  });
}


export function sendPasswordChangeCode() {
  return requestJson("/api/Profile/user/password/send-code", {
    method: "POST",
  });
}

export function changePasswordWithCode(code, newPassword, confirmNewPassword) {
  return requestJson("/api/Profile/user/password/change", {
    method: "POST",
    body: JSON.stringify({ code, newPassword, confirmNewPassword }),
  });
}

export function requestEmailChange(newEmail) {
  return requestJson("/api/Profile/user/email/request-change", {
    method: "POST",
    body: JSON.stringify({ newEmail }),
  });
}

export function confirmEmailChange(newEmail, code) {
  return requestJson("/api/Profile/user/email/confirm-change", {
    method: "POST",
    body: JSON.stringify({ newEmail, code }),
  });
}
