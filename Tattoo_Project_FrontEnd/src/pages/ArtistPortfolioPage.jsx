import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { getAllArtists } from "../api/artistApi";
import { readResponse } from "../api/http";
import ImageLightbox from "../components/ImageLightbox";
import UserAvatar from "../components/UserAvatar";
import { getEntityId } from "../utils/format";
import { getImageUrl } from "../utils/images";

function getPortfolioImageUrl(image) {
  return getImageUrl(image?.imageUrl || image?.ImageUrl || image?.url || image);
}

function ArtistPortfolioPage() {
  const { artistId } = useParams();
  const navigate = useNavigate();
  const [artist, setArtist] = useState(() => {
    try {
      const saved = JSON.parse(localStorage.getItem("selectedPortfolioArtist") || "null");
      return String(saved?.id || saved?.tattooArtistId) === String(artistId) ? saved : null;
    } catch {
      return null;
    }
  });
  const [selectedImage, setSelectedImage] = useState(null);
  const [loading, setLoading] = useState(!artist);
  const [error, setError] = useState("");

  useEffect(() => {
    let isMounted = true;

    async function loadArtist() {
      if (artist) return;
      setLoading(true);
      try {
        const response = await getAllArtists();
        const data = await readResponse(response);
        if (!response.ok) throw new Error(typeof data === "string" ? data : "Artist could not be loaded.");

        const match = (data || []).find((item, index) => String(getEntityId(item, index)) === String(artistId));
        if (!match) throw new Error("Artist not found.");
        if (isMounted) setArtist(match);
      } catch (err) {
        if (isMounted) setError(err.message || "Artist could not be loaded.");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    loadArtist();
    return () => { isMounted = false; };
  }, [artist, artistId]);

  const portfolio = useMemo(() => artist?.portfolioImages || artist?.PortfolioImages || [], [artist]);

  function handleStartRequest() {
    if (!artist) return;
    localStorage.setItem("selectedArtist", JSON.stringify({ ...artist, id: artistId }));
    navigate(`/create-tattoo-request/${artistId}`);
  }

  return (
    <main className="page-shell artist-portfolio-page">
      <section className="container">
        <div className="artist-portfolio-hero">
          <div className="artist-portfolio-identity">
            <UserAvatar
              firstName={artist?.firstName}
              lastName={artist?.lastName}
              email={artist?.email}
              imageUrl={artist?.profileImageUrl}
              size="xlarge"
            />
            <div>
              <p className="subtitle">Artist portfolio</p>
              <h1>{artist?.studioName || "Tattoo portfolio"}</h1>
              <p>{artist ? `${artist.firstName || ""} ${artist.lastName || ""}`.trim() : "Loading artist..."}</p>
              {(artist?.studioCity || artist?.studioCountry) && (
                <span className="artist-portfolio-location">{[artist.studioCity, artist.studioCountry].filter(Boolean).join(", ")}</span>
              )}
            </div>
          </div>
          <div className="artist-portfolio-hero-actions">
            <Link className="secondary-button" to="/explore">← Back to artists</Link>
            {artist && <button className="primary-button" type="button" onClick={handleStartRequest}>Start tattoo request</button>}
          </div>
        </div>

        {loading && <p className="message">Loading portfolio...</p>}
        {error && <p className="error">{error}</p>}

        {!loading && !error && (
          <>
            <div className="artist-portfolio-toolbar">
              <div>
                <p className="subtitle">Selected work</p>
                <h2>{portfolio.length} portfolio {portfolio.length === 1 ? "image" : "images"}</h2>
              </div>
              <p>Click any image to view it in full size.</p>
            </div>

            {portfolio.length > 0 ? (
              <div className="artist-portfolio-grid">
                {portfolio.map((image, index) => {
                  const imageUrl = getPortfolioImageUrl(image);
                  return (
                    <button
                      className="artist-portfolio-tile"
                      type="button"
                      key={`${imageUrl}-${index}`}
                      onClick={() => setSelectedImage(imageUrl)}
                    >
                      <img src={imageUrl} alt={`${artist?.studioName || "Artist"} portfolio ${index + 1}`} />
                      <span className="artist-portfolio-tile-overlay"><strong>View image</strong><small>{String(index + 1).padStart(2, "0")}</small></span>
                    </button>
                  );
                })}
              </div>
            ) : (
              <div className="artist-portfolio-empty">
                <span>IR</span>
                <h2>No portfolio images yet</h2>
                <p>This artist has not uploaded any work to their portfolio.</p>
              </div>
            )}
          </>
        )}
      </section>

      <ImageLightbox imageUrl={selectedImage} alt={`${artist?.studioName || "Artist"} portfolio`} onClose={() => setSelectedImage(null)} />
    </main>
  );
}

export default ArtistPortfolioPage;
