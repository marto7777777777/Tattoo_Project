import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { formatTime, getDayName, getEntityId, getScheduleTypeName } from "../utils/format";

function ArtistCard({
  artist,
  index,
  isFavorite = false,
  showFavoriteButton = false,
  onToggleFavorite,
}) {
  const navigate = useNavigate();
  const { isLoggedIn, isClient } = useAuth();
  const artistId = getEntityId(artist, index);
  const canUseArtistActions = Boolean(artist?.id || artist?.tattooArtistId);

  function handleCreateRequest() {
    if (!canUseArtistActions) {
      return;
    }

    localStorage.setItem("selectedArtist", JSON.stringify({ ...artist, id: artistId }));

    if (!isLoggedIn) {
      navigate("/login");
      return;
    }

    if (!isClient) {
      navigate("/create-client-profile");
      return;
    }

    navigate(`/create-tattoo-request/${artistId}`);
  }

  return (
    <article className="card artist-card">
      <div className="card-head">
        <div>
          <h2>{artist.studioName}</h2>
          <p className="subtitle inline-subtitle">
            {artist.firstName} {artist.lastName}
          </p>
        </div>

        <div className="card-badges">
          {artist.isVerified && <span className="status-pill status-completed">Verified</span>}
          {showFavoriteButton && (
            <button
              className={`heart-button ${isFavorite ? "heart-active" : ""}`}
              type="button"
              disabled={!canUseArtistActions}
              title={isFavorite ? "Remove from favorites" : "Add to favorites"}
              onClick={() => onToggleFavorite?.(artist, index)}
            >
              {isFavorite ? "♥" : "♡"}
            </button>
          )}
        </div>
      </div>

      <div className="rating-row">
        <span>⭐ {artist.averageRating || 0}</span>
        <span>{artist.reviewCount || 0} reviews</span>
      </div>

      <p className="muted">{artist.description}</p>

      <div className="info-list">
        <p><span>Address:</span> {artist.studioAddress}</p>
        {(artist.studioCity || artist.studioCountry) && (
          <p><span>Location:</span> {[artist.studioCity, artist.studioCountry].filter(Boolean).join(", ")}</p>
        )}
        <p><span>Phone:</span> {artist.phoneNumber}</p>
        <p><span>Online consultation:</span> {artist.offersOnlineConsultation ? "Yes" : "No"}</p>
        <p><span>Deposit:</span> {artist.requiresDeposit ? `${artist.depositAmount} BGN` : "Not required"}</p>
        <p><span>Consultation duration:</span> {artist.consultationDurationMinutes} minutes</p>
      </div>

      {artist.requirements?.length > 0 && (
        <div className="section">
          <h3>Requirements</h3>
          <ul className="muted">
            {artist.requirements.map((requirement, requirementIndex) => (
              <li key={requirementIndex}>{requirement.description}</li>
            ))}
          </ul>
        </div>
      )}

      {artist.schedules?.length > 0 && (
        <div className="section">
          <h3>Schedule</h3>
          <div className="small-list">
            {artist.schedules.map((schedule, scheduleIndex) => (
              <div className="small-list-row" key={scheduleIndex}>
                <span>{getDayName(schedule.dayOfWeek)}</span>
                <span>{formatTime(schedule.startTime)} - {formatTime(schedule.endTime)}</span>
                <span className="status-pill">{getScheduleTypeName(schedule.scheduleType)}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {artist.portfolioImages?.length > 0 && (
        <div className="section">
          <h3>Portfolio</h3>
          <div className="image-grid">
            {artist.portfolioImages.slice(0, 4).map((image, imageIndex) => (
              <img key={imageIndex} src={image.imageUrl} alt="Portfolio" />
            ))}
          </div>
        </div>
      )}

      <button className="primary-button full-button" onClick={handleCreateRequest} disabled={!canUseArtistActions}>
        Create Tattoo Request
      </button>
    </article>
  );
}

export default ArtistCard;
