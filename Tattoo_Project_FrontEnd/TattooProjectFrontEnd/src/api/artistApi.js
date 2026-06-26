
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