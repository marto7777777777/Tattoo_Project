import { apiRequest } from "./http";

export function registerUser(registerData) {
  return apiRequest("/api/Auth/register", {
    method: "POST",
    body: JSON.stringify(registerData),
  });
}

export function loginUser(loginData) {
  return apiRequest("/api/Auth/login", {
    method: "POST",
    body: JSON.stringify(loginData),
  });
}
