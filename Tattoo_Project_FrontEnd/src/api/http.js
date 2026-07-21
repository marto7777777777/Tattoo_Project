import { API_BASE_URL } from "./apiConfig";

export function getToken() {
  return localStorage.getItem("token");
}

export function setToken(token) {
  if (token) localStorage.setItem("token", token);
}

export function clearToken() {
  localStorage.removeItem("token");
}

export async function apiRequest(path, options = {}) {
  const token = getToken();
  const headers = {
    ...(options.body && !(options.body instanceof FormData)
      ? { "Content-Type": "application/json" }
      : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers || {}),
  };

  try {
    return await fetch(`${API_BASE_URL}${path}`, { ...options, headers });
  } catch (error) {
    throw new Error(`Cannot reach the backend at ${API_BASE_URL}. Make sure the API is running.`);
  }
}

export async function readResponse(response) {
  const contentType = response.headers.get("content-type") || "";
  if (response.status === 204) return null;
  if (contentType.includes("application/json")) return response.json();
  return response.text();
}

function getErrorMessage(data, response) {
  if (typeof data === "string" && data.trim()) return data;
  if (data?.errors) {
    const messages = Object.values(data.errors).flat().filter(Boolean);
    if (messages.length) return messages.join(" ");
  }
  return data?.message || data?.detail || data?.title || data?.error ||
    `Request failed (${response.status} ${response.statusText}).`;
}

export async function requestJson(path, options = {}) {
  const response = await apiRequest(path, options);
  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(getErrorMessage(data, response));
  }

  return data;
}
