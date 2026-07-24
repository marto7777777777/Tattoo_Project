import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getMyStudio, getStudioById } from "../api/studioApi";
import UserAvatar from "../components/UserAvatar";
import { useAuth } from "../context/AuthContext";
import { getImageUrl } from "../utils/images";

function StudioProfilePage() {
  const { studioId } = useParams();
  const navigate = useNavigate();
  const { isLoggedIn, isClient, isArtist } = useAuth();
  const [studio, setStudio] = useState(null);
  const [error, setError] = useState("");
  const [isMyStudio, setIsMyStudio] = useState(false);

  useEffect(() => {
    let active = true;
    getStudioById(studioId)
      .then((data) => active && setStudio(data))
      .catch((err) => active && setError(err.message || "Could not load studio."));
    return () => { active = false; };
  }, [studioId]);

  useEffect(() => {
    if (!isArtist) { setIsMyStudio(false); return; }
    getMyStudio()
      .then((data) => setIsMyStudio(Boolean(data?.hasStudio && Number(data.studio?.id) === Number(studioId))))
      .catch(() => setIsMyStudio(false));
  }, [isArtist, studioId]);

  function enrichArtist(artist) {
    return {
      ...artist,
      studioId: studio.id,
      studioName: studio.name,
      studioAddress: studio.address,
      studioCity: studio.city,
      studioCountry: studio.country,
      portfolioImages: (artist.portfolioImageUrls || []).map((imageUrl, index) => ({ id: index + 1, imageUrl })),
    };
  }

  function startRequest(artist) {
    localStorage.setItem("selectedArtist", JSON.stringify(enrichArtist(artist)));
    if (!isLoggedIn) return navigate("/login");
    if (!isClient) {
      return navigate(`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(`/create-tattoo-request/${artist.id}`)}`);
    }
    navigate(`/create-tattoo-request/${artist.id}`);
  }

  if (error) return <main className="page-shell"><section className="container"><p className="error">{error}</p></section></main>;
  if (!studio) return <main className="page-shell"><section className="container"><p className="message">Loading studio...</p></section></main>;

  return (
    <main className="page-shell">
      <section className="container">
        <div className="studio-profile-hero">
          <div>
            <p className="subtitle">Studio profile</p>
            <h1>{studio.name}</h1>
            <p className="studio-profile-description">{studio.description}</p>
          </div>
          <div className="studio-location-card">
            <strong>{studio.address}</strong>
            <span>{[studio.city, studio.country].filter(Boolean).join(", ")}</span>
            <small>{studio.artistCount} artist{studio.artistCount === 1 ? "" : "s"}</small>
          </div>
        </div>

        <div className="section-heading studio-section-heading">
          <div>
            <p className="subtitle inline-subtitle">Our artists</p>
            <h2>Choose the artist for your tattoo</h2>
          </div>
          <p className="muted">Each artist keeps their own portfolio, schedule, consultations and tattoo sessions.</p>
        </div>

        <div className="studio-members-public-grid">
          {(studio.artists || []).map((artist) => (
            <article className="studio-public-artist-card" key={artist.id}>
              <UserAvatar
                firstName={artist.firstName}
                lastName={artist.lastName}
                imageUrl={artist.profileImageUrl}
                size="xlarge"
              />
              <div className="studio-public-artist-copy">
                <div className="studio-public-artist-title">
                  <div>
                    <h3>{artist.firstName} {artist.lastName}</h3>
                    <p>{artist.description}</p>
                  </div>
                  <span className="rating-chip">★ {artist.averageRating || "New"}</span>
                </div>

                <div className="studio-public-portfolio">
                  {(artist.portfolioImageUrls || []).slice(0, 3).map((url, index) => (
                    <img key={`${url}-${index}`} src={getImageUrl(url)} alt={`${artist.firstName} portfolio ${index + 1}`} />
                  ))}
                </div>

                <div className="studio-public-artist-actions">
                  <button className="secondary-button" type="button" onClick={() => { localStorage.setItem("selectedPortfolioArtist", JSON.stringify(enrichArtist(artist))); navigate(`/artists/${artist.id}/portfolio`); }}>
                    View portfolio
                  </button>
                  {isMyStudio ? (
                    <button className="primary-button" type="button" onClick={() => navigate("/my-studio")}>My Studio</button>
                  ) : (
                    <button className="primary-button" type="button" onClick={() => startRequest(artist)}>Start tattoo request</button>
                  )}
                </div>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

export default StudioProfilePage;
