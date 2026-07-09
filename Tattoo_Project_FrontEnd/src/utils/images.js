import { API_BASE_URL } from "../api/apiConfig";

export function getImageUrl(imageUrl) {
  if (!imageUrl) return "";
  if (imageUrl.startsWith("http://") || imageUrl.startsWith("https://") || imageUrl.startsWith("data:")) {
    return imageUrl;
  }
  return `${API_BASE_URL}${imageUrl}`;
}
