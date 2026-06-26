
import { API_BASE_URL } from "./apiConfig";

export async function createClientProfile(clientData) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/Client/profile`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(clientData),
  });

  return response;
}