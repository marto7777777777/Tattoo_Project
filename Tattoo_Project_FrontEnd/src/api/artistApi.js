import { API_BASE_URL } from "./apiConfig";

export async function createArtistProfile(artistData) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/TattooArtist/profile`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(artistData),
  });

  return response;
}

export async function getAllArtists() {
  const response = await fetch(`${API_BASE_URL}/api/TattooArtist`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  });

  return response;
}

export async function searchArtists(query) {
  const response = await fetch(
    `${API_BASE_URL}/api/TattooArtist/search?query=${encodeURIComponent(query)}`,
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    }
  );

  return response;
}

export async function getRecommendedArtists() {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/TattooArtist/recommended`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  return response;
}
