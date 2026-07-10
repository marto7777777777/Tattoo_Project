import { useEffect, useMemo, useState } from "react";
import ArtistCard from "../components/ArtistCard";
import { getAllArtists, getRecommendedArtists, searchArtists } from "../api/artistApi";
import { addFavoriteArtist, getMyFavoriteArtists, removeFavoriteArtist } from "../api/favoriteArtistApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { getMyProfile } from "../api/profileApi";
import { getEntityId } from "../utils/format";

function ArtistsPage() {
  const { isLoggedIn, isClient, isArtist } = useAuth();
  const [myProfile, setMyProfile] = useState(null);
  const [artists, setArtists] = useState([]);
  const [favoriteIds, setFavoriteIds] = useState([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [title, setTitle] = useState("Recommended tattoo artists");

  const favoriteIdSet = useMemo(() => new Set(favoriteIds.map(String)), [favoriteIds]);

  const visibleArtists = useMemo(() => {
    return filterMyArtist(artists);
  }, [artists, isLoggedIn, isArtist, myProfile]);

  useEffect(() => {
    if (isLoggedIn) {
      getMyProfile().then(setMyProfile).catch(() => setMyProfile(null));
    } else {
      setMyProfile(null);
    }
    loadInitialArtists();
  }, [isLoggedIn, isClient]);

  async function loadInitialArtists() {
    setIsLoading(true);
    setError("");
    setSuccess("");

    try {
      let response;

      if (isLoggedIn && isClient) {
        response = await getRecommendedArtists();
        setTitle("Recommended tattoo artists near you");
      } else {
        response = await getAllArtists();
        setTitle("Tattoo artists");
      }

      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setArtists(data || []);

      if (isLoggedIn && isClient) {
        await loadFavoriteIds();
      }
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }


  function filterMyArtist(items) {
    if (!isLoggedIn || !isArtist || !myProfile?.email) {
      return items;
    }

    return items.filter((artist) => String(artist.email || "").toLowerCase() !== String(myProfile.email).toLowerCase());
  }

  async function loadFavoriteIds() {
    try {
      const response = await getMyFavoriteArtists();
      const data = await readResponse(response);

      if (!response.ok) return;

      const ids = (data || [])
        .map((artist, index) => getEntityId(artist, index))
        .filter(Boolean);

      setFavoriteIds(ids);
    } catch {
      // Favorites are helpful, but the page can still work without them.
    }
  }

  async function handleSearch(event) {
    event.preventDefault();
    setError("");
    setSuccess("");
    setIsLoading(true);

    try {
      if (!query.trim()) {
        await loadInitialArtists();
        return;
      }

      const response = await searchArtists(query.trim());
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setArtists(data || []);
      setTitle(`Search results for "${query.trim()}"`);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleToggleFavorite(artist, index) {
    if (!isLoggedIn) {
      setError("Login first to add favorite artists.");
      return;
    }

    if (!isClient) {
      setError("Only clients can add favorite artists.");
      return;
    }

    const artistId = getEntityId(artist, index);

    if (!artist?.id && !artist?.tattooArtistId) {
      setError("Cannot perform this action for this artist. Please refresh the page and try again.");
      return;
    }

    const isFavorite = favoriteIdSet.has(String(artistId));
    setError("");
    setSuccess("");

    try {
      const response = isFavorite
        ? await removeFavoriteArtist(artistId)
        : await addFavoriteArtist(artistId);

      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      if (isFavorite) {
        setFavoriteIds((current) => current.filter((id) => String(id) !== String(artistId)));
        setSuccess("Artist removed from favorites.");
      } else {
        setFavoriteIds((current) => [...current, artistId]);
        setSuccess("Artist added to favorites.");
      }
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">Tattoo Artists</p>
          <h1>{title}</h1>
          <p>
            Search by artist name, studio name, city, country, or address. Logged clients see recommended artists from their country first.
          </p>
        </div>

        <form className="search-bar" onSubmit={handleSearch}>
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search Plovdiv, Bulgaria, studio name, artist name..."
          />
          <button className="primary-button" type="submit">Search</button>
          <button className="secondary-button" type="button" onClick={() => { setQuery(""); loadInitialArtists(); }}>
            Reset
          </button>
        </form>

        {isLoading && <p className="message">Loading artists...</p>}
        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}
        {!isLoading && !error && visibleArtists.length === 0 && <p className="message">No tattoo artists found.</p>}

        <div className="grid-2">
          {visibleArtists.map((artist, index) => {
            const artistId = getEntityId(artist, index);

            return (
              <ArtistCard
                artist={artist}
                index={index}
                key={`${artistId}-${index}`}
                isFavorite={favoriteIdSet.has(String(artistId))}
                showFavoriteButton={isLoggedIn && isClient}
                onToggleFavorite={handleToggleFavorite}
              />
            );
          })}
        </div>
      </section>
    </main>
  );
}

export default ArtistsPage;
