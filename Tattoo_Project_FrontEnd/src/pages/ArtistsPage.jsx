import { useEffect, useState } from "react";
import StudioCard from "../components/StudioCard";
import { getMyStudio, getStudios } from "../api/studioApi";
import { useAuth } from "../context/AuthContext";
import { addFavoriteStudio, getMyFavoriteStudios, removeFavoriteStudio } from "../api/favoriteStudioApi";
import { readResponse } from "../api/http";

function ArtistsPage() {
  const { isArtist, isClient } = useAuth();
  const [studios, setStudios] = useState([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [myStudioId, setMyStudioId] = useState(null);
  const [favoriteIds, setFavoriteIds] = useState(new Set());

  async function loadStudios(search = "") {
    setLoading(true);
    setError("");
    try {
      const data = await getStudios(search);
      setStudios(Array.isArray(data) ? data : []);
    } catch (err) {
      setStudios([]);
      setError(err.message || "Could not load studios.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { loadStudios(); }, []);

  useEffect(() => {
    if (!isArtist) { setMyStudioId(null); return; }
    getMyStudio()
      .then((data) => setMyStudioId(data?.hasStudio ? Number(data.studio?.id) : null))
      .catch(() => setMyStudioId(null));
  }, [isArtist]);

  useEffect(() => {
    if (!isClient) { setFavoriteIds(new Set()); return; }
    getMyFavoriteStudios()
      .then(async (response) => {
        const data = await readResponse(response);
        if (response.ok) setFavoriteIds(new Set((data || []).map((studio) => Number(studio.id))));
      })
      .catch(() => {});
  }, [isClient]);

  async function toggleFavorite(studio) {
    const studioId = Number(studio.id);
    const isFavorite = favoriteIds.has(studioId);
    try {
      const response = await (isFavorite ? removeFavoriteStudio(studioId) : addFavoriteStudio(studioId));
      const data = await readResponse(response);
      if (!response.ok) throw new Error(typeof data === "string" ? data : "Could not update favorites.");
      setFavoriteIds((current) => {
        const next = new Set(current);
        if (isFavorite) next.delete(studioId); else next.add(studioId);
        return next;
      });
    } catch (err) {
      setError(err.message);
    }
  }

  function handleSearch(event) {
    event.preventDefault();
    loadStudios(query);
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">Explore studios</p>
          <h1>Find a studio, then choose your artist</h1>
          <p>Browse tattoo studios and compare the individual artists working inside each one. Every artist keeps their own portfolio, description and availability.</p>
        </div>

        <form className="search-bar" onSubmit={handleSearch}>
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search studio name, city or country..." />
          <button className="primary-button" type="submit">Search</button>
          <button className="secondary-button" type="button" onClick={() => { setQuery(""); loadStudios(); }}>Reset</button>
        </form>

        {loading && <p className="message">Loading studios...</p>}
        {error && <p className="error">{error}</p>}
        {!loading && !error && studios.length === 0 && <p className="message">No studios found.</p>}

        <div className="studio-explore-grid">
          {studios.map((studio) => (
            <StudioCard
              key={studio.id}
              studio={studio}
              isMyStudio={Number(studio.id) === myStudioId}
              isFavorite={favoriteIds.has(Number(studio.id))}
              showFavoriteButton={isClient}
              onToggleFavorite={toggleFavorite}
            />
          ))}
        </div>
      </section>
    </main>
  );
}

export default ArtistsPage;
