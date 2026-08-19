const STORAGE_KEY = "inkroute.pendingArtistRequest";
const MAX_AGE_MS = 24 * 60 * 60 * 1000;

export function rememberArtistRequest(artistId) {
  if (Number.isInteger(Number(artistId)) && Number(artistId) > 0) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      artistId: Number(artistId),
      createdAt: Date.now(),
    }));
  }
}

export function getPendingArtistRequestPath() {
  let pending;
  try {
    pending = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
  } catch {
    clearPendingArtistRequest();
    return null;
  }
  const age = Date.now() - Number(pending?.createdAt);
  if (!pending || !Number.isFinite(age) || age < 0 || age > MAX_AGE_MS) {
    clearPendingArtistRequest();
    return null;
  }
  const artistId = Number(pending.artistId);
  if (!Number.isInteger(artistId) || artistId <= 0) {
    clearPendingArtistRequest();
    return null;
  }
  return `/create-tattoo-request/${artistId}`;
}

export function clearPendingArtistRequest() {
  localStorage.removeItem(STORAGE_KEY);
}
