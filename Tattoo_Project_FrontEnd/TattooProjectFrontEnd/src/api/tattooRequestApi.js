import { API_BASE_URL } from "./apiConfig";

export async function createTattooRequest(requestData) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/TattooRequest`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(requestData),
  });

  return response;
}

export async function getMyTattooRequests() {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/TattooRequest/my-requests`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  return response;
}