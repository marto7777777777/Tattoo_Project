import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getPublicArtist } from "../api/artistApi";
import UserAvatar from "../components/UserAvatar";
import { useAuth } from "../context/AuthContext";
import { getImageUrl } from "../utils/images";
import { clearPendingArtistRequest, rememberArtistRequest } from "../utils/pendingArtistRequest";

function PublicArtistPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const { isLoggedIn, isClient } = useAuth();
  const [artist, setArtist] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    setLoading(true);
    getPublicArtist(slug)
      .then((result) => { if (active) setArtist(result); })
      .catch((err) => { if (active) setError(err.message || "This artist profile could not be found."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [slug]);

  function sendRequest() {
    if (!artist?.id) return;
    localStorage.setItem("selectedArtist", JSON.stringify({
      id: artist.id,
      firstName: artist.firstName,
      lastName: artist.lastName,
      studioName: artist.studioName,
      studioCity: artist.studioCity,
      studioCountry: artist.studioCountry,
      profileImageUrl: artist.profileImageUrl,
    }));
    rememberArtistRequest(artist.id);
    const requestPath = `/create-tattoo-request/${artist.id}`;
    if (!isLoggedIn) return navigate("/login");
    if (!isClient) return navigate(`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(requestPath)}`);
    clearPendingArtistRequest();
    navigate(requestPath);
  }

  if (loading) return <main className="page-shell"><section className="container"><p className="message">Loading artist profile...</p></section></main>;
  if (error || !artist) return <main className="page-shell"><section className="container"><div className="card form-card public-artist-missing"><h1>Artist profile unavailable</h1><p>{error || "This artist profile could not be found."}</p><button className="secondary-button" onClick={() => navigate("/explore")}>Explore studios</button></div></section></main>;

  const fullName = `${artist.firstName} ${artist.lastName}`.trim();
  const location = [artist.studioCity, artist.studioCountry].filter(Boolean).join(", ");

  return (
    <main className="page-shell public-artist-page">
      <section className="container public-artist-container">
        <header className="public-artist-hero">
          <div className="public-artist-identity">
            <UserAvatar firstName={artist.firstName} lastName={artist.lastName} imageUrl={artist.profileImageUrl} size="xlarge" />
            <div><p className="subtitle">Tattoo artist</p><h1>{fullName}</h1><p className="public-artist-studio">{artist.studioName}{location ? ` · ${location}` : ""}</p></div>
          </div>
          <button className="primary-button public-artist-request-button" type="button" onClick={sendRequest}>Send request</button>
        </header>

        <div className="public-artist-grid">
          <section className="card form-card public-artist-about"><p className="subtitle inline-subtitle">About the artist</p><h2>Important information</h2><p>{artist.description}</p>
            {artist.specialtyStyles?.length > 0 && <div className="public-style-list">{artist.specialtyStyles.map((style) => <span key={style}>{style}</span>)}</div>}
          </section>
          <section className="card form-card public-artist-details"><p className="subtitle inline-subtitle">Studio</p><h2>{artist.studioName}</h2><p>{artist.studioAddress}</p><p>{location}</p>
            {artist.phoneNumber && <a className="public-artist-phone" href={`tel:${artist.phoneNumber}`}><span>Phone number</span><strong>{artist.phoneNumber}</strong></a>}
            {artist.reviewCount > 0 && <div className="rating-chip">★ {artist.averageRating} · {artist.reviewCount} reviews</div>}
          </section>
        </div>

        {artist.portfolioImages?.length > 0 && <section className="public-portfolio-section"><div className="section-heading"><p className="subtitle inline-subtitle">Selected work</p><h2>Portfolio</h2></div><div className="public-portfolio-grid">{artist.portfolioImages.map((image, index) => <img key={image.id || image.imageUrl} src={getImageUrl(image.imageUrl)} alt={`${fullName} portfolio ${index + 1}`} loading="lazy" />)}</div></section>}

        {artist.requirements?.length > 0 && <section className="card form-card public-requirements"><p className="subtitle inline-subtitle">Before you send a request</p><h2>Artist requirements</h2><ul>{artist.requirements.map((requirement) => <li key={requirement.id || requirement.description}>{requirement.description}</li>)}</ul></section>}

        <section className="public-artist-cta"><div><p className="subtitle">Start your tattoo project</p><h2><span>Send your idea directly to</span> {artist.firstName}</h2><p>InkRoute will keep your request, consultation and tattoo sessions organized in one place.</p></div><button className="primary-button" type="button" onClick={sendRequest}>Send request</button></section>
      </section>
    </main>
  );
}

export default PublicArtistPage;
