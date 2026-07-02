import { apiRequest } from "./http";

export function createClientProfile(clientData) {
  return apiRequest("/api/Client/profile", {
    method: "POST",
    body: JSON.stringify(clientData),
  });
}

export function updateClientProfile(clientData) {
  return apiRequest("/api/Client/profile", {
    method: "PUT",
    body: JSON.stringify(clientData),
  });
}
