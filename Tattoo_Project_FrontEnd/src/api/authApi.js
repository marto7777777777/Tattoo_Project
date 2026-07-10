import { apiRequest, requestJson } from "./http";

export function registerUser(registerData) {
  return apiRequest("/api/Auth/register", {
    method: "POST",
    body: JSON.stringify(registerData),
  });
}

export function verifyRegisterCode(email, code) {
  return requestJson("/api/Auth/register/verify-code", {
    method: "POST",
    body: JSON.stringify({ email, code }),
  });
}

export function resendRegisterCode(email) {
  return requestJson("/api/Auth/register/resend-code", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function loginUser(loginData) {
  return apiRequest("/api/Auth/login", {
    method: "POST",
    body: JSON.stringify(loginData),
  });
}

export function sendForgotPasswordCode(email) {
  return requestJson("/api/Auth/forgot-password/send-code", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function verifyForgotPasswordCode(email, code) {
  return requestJson("/api/Auth/forgot-password/verify-code", {
    method: "POST",
    body: JSON.stringify({ email, code }),
  });
}

export function resetForgottenPassword(email, code, newPassword, confirmNewPassword) {
  return requestJson("/api/Auth/forgot-password/reset", {
    method: "POST",
    body: JSON.stringify({ email, code, newPassword, confirmNewPassword }),
  });
}
