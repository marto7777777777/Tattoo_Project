import { API_BASE_URL } from "./apiConfig";

export function getToken() {
  return localStorage.getItem("token");
}

export function setToken(token) {
  if (token) {
    localStorage.setItem("token", token);
  }
}

export function clearToken() {
  localStorage.removeItem("token");
}

export async function apiRequest(path, options = {}) {
  const token = getToken();

  const headers = {
    ...(options.body ? { "Content-Type": "application/json" } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers || {}),
  };

  return fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
  });
}

export async function readResponse(response) {
  const contentType = response.headers.get("content-type") || "";

  if (contentType.includes("application/json")) {
    return response.json();
  }

  return response.text();
}

export async function requestJson(path, options = {}) {
  const response = await apiRequest(path, options);
  const data = await readResponse(response);

  if (!response.ok) {
    const message = typeof data === "string" ? data : JSON.stringify(data);
    throw new Error(message || "Request failed.");
  }

  return data;
}
