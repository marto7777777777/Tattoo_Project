import { API_BASE_URL } from "./apiConfig";

export async function createArtistReview(reviewData) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/ArtistReview`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(reviewData),
  });

  return response;
}

export async function getArtistReviews(tattooArtistId) {
  const response = await fetch(`${API_BASE_URL}/api/ArtistReview/artist/${tattooArtistId}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  });

  return response;
}
