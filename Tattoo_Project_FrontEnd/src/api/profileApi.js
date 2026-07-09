import { API_BASE_URL } from "./apiConfig";
import { getToken, requestJson } from "./http";

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

export async function updateProfileImage(file) {
  const token = getToken();
  const formData = new FormData();
  formData.append("image", file);

  const response = await fetch(`${API_BASE_URL}/api/Profile/contact/profile-image`, {
    method: "PATCH",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
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
  const token = getToken();
  const formData = new FormData();
  formData.append("image", file);

  const response = await fetch(`${API_BASE_URL}/api/Profile/portfolio/images`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export function deletePortfolioImage(id) {
  return requestJson(`/api/Profile/portfolio/images/${id}`, {
    method: "DELETE",
  });
}
