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

export class ApiError extends Error {
  constructor(message, { status = 0, code = "", data = null, networkError = false } = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.data = data;
    this.networkError = networkError;
  }
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
    if (error instanceof ApiError) throw error;
    throw new ApiError(
      "The connection was interrupted. InkRoute will check whether the operation completed.",
      { networkError: true }
    );
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
    throw new ApiError(getErrorMessage(data, response), {
      status: response.status,
      code: data?.code || "",
      data,
    });
  }

  return data;
}
