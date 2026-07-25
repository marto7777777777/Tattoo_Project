import { API_BASE_URL } from "./apiConfig";
import { getToken } from "./http";

function headers() {
  return { Authorization: `Bearer ${getToken()}` };
}

export function addFavoriteStudio(studioId) {
  return fetch(`${API_BASE_URL}/api/ClientFavoriteStudio/${studioId}`, {
    method: "POST",
    headers: headers(),
  });
}

export function removeFavoriteStudio(studioId) {
  return fetch(`${API_BASE_URL}/api/ClientFavoriteStudio/${studioId}`, {
    method: "DELETE",
    headers: headers(),
  });
}

export function getMyFavoriteStudios() {
  return fetch(`${API_BASE_URL}/api/ClientFavoriteStudio/my-favorites`, {
    headers: headers(),
  });
}
