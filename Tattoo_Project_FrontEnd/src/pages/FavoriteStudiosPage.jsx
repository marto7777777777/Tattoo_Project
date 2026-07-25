import { useEffect, useState } from "react";
import StudioCard from "../components/StudioCard";
import { getMyFavoriteStudios, removeFavoriteStudio } from "../api/favoriteStudioApi";
import { readResponse } from "../api/http";

function FavoriteStudiosPage() {
  const [studios, setStudios] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyFavoriteStudios()
      .then(async (response) => {
        const data = await readResponse(response);
        if (!response.ok) throw new Error(typeof data === "string" ? data : "Could not load favorite studios.");
        setStudios(Array.isArray(data) ? data : []);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  async function remove(studio) {
    setError("");
    setSuccess("");
    try {
      const response = await removeFavoriteStudio(studio.id);
      const data = await readResponse(response);
      if (!response.ok) throw new Error(typeof data === "string" ? data : "Could not remove studio.");
      setStudios((current) => current.filter((item) => item.id !== studio.id));
      setSuccess("Studio removed from favorites.");
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <main className="page-shell favorite-studios-page">
      <section className="container">
        <div className="header">
          <p className="subtitle">Favorite studios</p>
          <h1>Your saved tattoo studios</h1>
          <p>Keep studios here while you compare their artists, portfolios and locations.</p>
        </div>
        {loading && <p className="message">Loading favorite studios...</p>}
        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}
        {!loading && !error && studios.length === 0 && <p className="message">You have no favorite studios yet.</p>}
        <div className="studio-explore-grid favorite-studios-grid">
          {studios.map((studio) => (
            <StudioCard
              key={studio.id}
              studio={studio}
              isFavorite
              showFavoriteButton
              onToggleFavorite={() => remove(studio)}
            />
          ))}
        </div>
      </section>
    </main>
  );
}

export default FavoriteStudiosPage;
