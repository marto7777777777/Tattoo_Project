import { API_BASE_URL } from "./apiConfig";

export async function addFavoriteArtist(tattooArtistId) {
  const token = localStorage.getItem("token");

  const response = await fetch(
    `${API_BASE_URL}/api/ClientFavoriteArtist/${tattooArtistId}`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    }
  );

  return response;
}

export async function removeFavoriteArtist(tattooArtistId) {
  const token = localStorage.getItem("token");

  const response = await fetch(
    `${API_BASE_URL}/api/ClientFavoriteArtist/${tattooArtistId}`,
    {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    }
  );

  return response;
}

export async function getMyFavoriteArtists() {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/ClientFavoriteArtist/my-favorites`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  return response;
}
