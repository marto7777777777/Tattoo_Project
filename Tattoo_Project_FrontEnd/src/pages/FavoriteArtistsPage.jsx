import { useEffect, useMemo, useState } from "react";
import ArtistCard from "../components/ArtistCard";
import { getMyFavoriteArtists, removeFavoriteArtist } from "../api/favoriteArtistApi";
import { readResponse } from "../api/http";
import { getEntityId } from "../utils/format";

function FavoriteArtistsPage() {
  const [artists, setArtists] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const favoriteIdSet = useMemo(
    () => new Set(artists.map((artist, index) => String(getEntityId(artist, index)))),
    [artists]
  );

  useEffect(() => {
    loadFavorites();
  }, []);

  async function loadFavorites() {
    setIsLoading(true);
    setError("");
    setSuccess("");

    try {
      const response = await getMyFavoriteArtists();
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setArtists(data || []);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleRemoveFavorite(artist, index) {
    const artistId = getEntityId(artist, index);

    if (!artist?.id && !artist?.tattooArtistId) {
      setError("Cannot remove this artist right now. Please refresh the page and try again.");
      return;
    }

    setError("");
    setSuccess("");

    try {
      const response = await removeFavoriteArtist(artistId);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setArtists((current) => current.filter((item, itemIndex) => String(getEntityId(item, itemIndex)) !== String(artistId)));
      setSuccess("Artist removed from favorites.");
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">Favorite Artists</p>
          <h1>Your saved tattoo artists</h1>
          <p>Keep artists here while you compare studios, ratings, schedules, and portfolio images.</p>
        </div>

        {isLoading && <p className="message">Loading favorite artists...</p>}
        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}
        {!isLoading && !error && artists.length === 0 && <p className="message">You have no favorite artists yet.</p>}

        <div className="grid-2">
          {artists.map((artist, index) => {
            const artistId = getEntityId(artist, index);

            return (
              <ArtistCard
                artist={artist}
                index={index}
                key={`${artistId}-${index}`}
                isFavorite={favoriteIdSet.has(String(artistId))}
                showFavoriteButton
                onToggleFavorite={handleRemoveFavorite}
              />
            );
          })}
        </div>
      </section>
    </main>
  );
}

export default FavoriteArtistsPage;
