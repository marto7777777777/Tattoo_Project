import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import UserAvatar from "./UserAvatar";
import ImageLightbox from "./ImageLightbox";
import { getImageUrl } from "../utils/images";
import { getEntityId } from "../utils/format";

function getPortfolioImageUrl(image) {
  return getImageUrl(image?.imageUrl || image?.ImageUrl || image?.url || image);
}

function ArtistCard({ artist, index, isFavorite = false, showFavoriteButton = false, onToggleFavorite }) {
  const navigate = useNavigate();
  const { isLoggedIn, isClient } = useAuth();
  const [previewImage, setPreviewImage] = useState(null);
  const artistId = getEntityId(artist, index);
  const canUseArtistActions = Boolean(artist?.id || artist?.tattooArtistId);
  const portfolio = artist.portfolioImages || artist.PortfolioImages || [];
  const previewImages = portfolio.slice(0, 3);

  function handleCreateRequest() {
    if (!canUseArtistActions) return;
    localStorage.setItem("selectedArtist", JSON.stringify({ ...artist, id: artistId }));
    if (!isLoggedIn) return navigate("/login");
    if (!isClient) return navigate(`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(`/create-tattoo-request/${artistId}`)}`);
    navigate(`/create-tattoo-request/${artistId}`);
  }

  function handleOpenPortfolio() {
    localStorage.setItem("selectedPortfolioArtist", JSON.stringify({ ...artist, id: artistId }));
    navigate(`/artists/${artistId}/portfolio`);
  }

  return (
    <>
      <article className="artist-card-v2 artist-card-profile-first">
        <div className="artist-card-profile-head">
          <div className="artist-card-floating-actions">
            {artist.isVerified && <span className="verified-badge">✓ Verified</span>}
            {showFavoriteButton && (
              <button
                className={`heart-button ${isFavorite ? "heart-active" : ""}`}
                type="button"
                onClick={() => onToggleFavorite?.(artist, index)}
                aria-label={isFavorite ? "Remove from saved artists" : "Save artist"}
              >
                {isFavorite ? "♥" : "♡"}
              </button>
            )}
          </div>

          <UserAvatar
            firstName={artist.firstName}
            lastName={artist.lastName}
            email={artist.email}
            imageUrl={artist.profileImageUrl}
            size="xlarge"
            className="artist-card-main-avatar"
          />

          <div className="artist-card-centered-identity">
            <h2>{artist.firstName} {artist.lastName}</h2>
            <p>{artist.studioName || "Independent tattoo artist"}</p>
          </div>
        </div>

        <div className="artist-card-body">
          <div className="artist-meta-row artist-meta-row-centered">
            <span className="rating-chip">★ {artist.averageRating || "New"}</span>
            <span>{artist.reviewCount || 0} reviews</span>
            {(artist.studioCity || artist.studioCountry) && (
              <span>{[artist.studioCity, artist.studioCountry].filter(Boolean).join(", ")}</span>
            )}
          </div>

          <p className="artist-description artist-description-centered">
            {artist.description || "Tattoo artist profile and portfolio."}
          </p>

          <div className="artist-service-chips artist-service-chips-centered">
            {artist.offersOnlineConsultation && <span>Online consultation</span>}
            <span>{artist.consultationDurationMinutes || 30} min consultation</span>
            <span>{artist.requiresDeposit ? `${artist.depositAmount} BGN deposit` : "No deposit"}</span>
          </div>

          <section className="artist-mini-portfolio" aria-label="Portfolio preview">
            <div className="artist-mini-portfolio-head">
              <div>
                <span className="subtitle">Portfolio</span>
                <strong>{portfolio.length} {portfolio.length === 1 ? "image" : "images"}</strong>
              </div>
              <button className="artist-mini-portfolio-link" type="button" onClick={handleOpenPortfolio}>
                View all <span>→</span>
              </button>
            </div>

            <div className="artist-mini-portfolio-grid">
              {previewImages.length > 0 ? (
                previewImages.map((image, imageIndex) => {
                  const imageUrl = getPortfolioImageUrl(image);
                  return (
                    <button
                      className="artist-mini-portfolio-tile"
                      type="button"
                      key={`${imageUrl}-${imageIndex}`}
                      onClick={() => setPreviewImage(imageUrl)}
                      aria-label={`Open portfolio image ${imageIndex + 1}`}
                    >
                      <img src={imageUrl} alt={`${artist.studioName || artist.firstName} portfolio ${imageIndex + 1}`} />
                      <span className="artist-mini-portfolio-zoom">↗</span>
                    </button>
                  );
                })
              ) : (
                <button className="artist-mini-portfolio-empty" type="button" onClick={handleOpenPortfolio}>
                  <span>IR</span>
                  <small>Portfolio coming soon</small>
                </button>
              )}
            </div>
          </section>

          <div className="artist-card-actions artist-card-actions-single">
            <button className="primary-button" type="button" onClick={handleCreateRequest} disabled={!canUseArtistActions}>
              Start tattoo request <span>→</span>
            </button>
          </div>
        </div>
      </article>

      <ImageLightbox imageUrl={previewImage} alt={`${artist.studioName || artist.firstName} portfolio`} onClose={() => setPreviewImage(null)} />
    </>
  );
}

export default ArtistCard;
