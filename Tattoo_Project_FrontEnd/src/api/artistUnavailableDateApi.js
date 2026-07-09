import { API_BASE_URL } from "./apiConfig";

export async function createUnavailableDate(unavailableDateData) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/ArtistUnavailableDate`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(unavailableDateData),
  });

  return response;
}

export async function getMyUnavailableDates() {
  const token = localStorage.getItem("token");

  const response = await fetch(
    `${API_BASE_URL}/api/ArtistUnavailableDate/my-unavailable-dates`,
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    }
  );

  return response;
}

export async function deleteUnavailableDate(id) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE_URL}/api/ArtistUnavailableDate/${id}`, {
    method: "DELETE",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
  });

  return response;
}
