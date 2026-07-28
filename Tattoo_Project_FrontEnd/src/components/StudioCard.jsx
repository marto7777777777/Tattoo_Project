import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getImageUrl } from "../utils/images";

function initials(name = "") {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return parts.slice(0, 2).map((part) => part[0]?.toUpperCase()).join("") || "ST";
}

function StudioCard({
  studio,
  isMyStudio = false,
  isFavorite = false,
  showFavoriteButton = false,
  onToggleFavorite,
}) {
  const navigate = useNavigate();
  const trackRef = useRef(null);
  const [canLeft, setCanLeft] = useState(false);
  const [canRight, setCanRight] = useState(false);
  const artists = studio?.artists || studio?.Artists || [];
  const previews = studio?.portfolioPreviewUrls || studio?.PortfolioPreviewUrls || [];
  const styles = studio?.specialtyStyles || studio?.SpecialtyStyles || [];
  const reviewCount = Number(studio?.reviewCount ?? studio?.ReviewCount ?? 0);
  const rating = studio?.averageRating ?? studio?.AverageRating;
  const coverUrl = studio?.coverImageUrl || studio?.CoverImageUrl;
  const logoUrl = studio?.logoImageUrl || studio?.LogoImageUrl;
  const verified = artists.some((artist) => artist.isVerified || artist.IsVerified);

  function syncArrows() {
    const track = trackRef.current;
    if (!track) return;
    const max = Math.max(0, track.scrollWidth - track.clientWidth);
    setCanLeft(track.scrollLeft > 2);
    setCanRight(track.scrollLeft < max - 2);
  }

  useEffect(() => {
    syncArrows();
    const track = trackRef.current;
    if (!track) return undefined;
    const observer = new ResizeObserver(syncArrows);
    observer.observe(track);
    window.addEventListener("resize", syncArrows);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", syncArrows);
    };
  }, [previews.length]);

  function move(direction) {
    const track = trackRef.current;
    if (!track) return;
    const item = track.querySelector(".studio-portfolio-slide");
    const step = item ? item.getBoundingClientRect().width + 10 : track.clientWidth;
    const max = Math.max(0, track.scrollWidth - track.clientWidth);
    track.scrollTo({
      left: Math.max(0, Math.min(max, track.scrollLeft + direction * step)),
      behavior: "smooth",
    });
  }

  return (
    <article className="studio-card studio-card-premium">
      <div className={`studio-card-cover ${coverUrl ? "" : "studio-card-cover-empty"}`}>
        {coverUrl && <img src={getImageUrl(coverUrl)} alt={`${studio.name} studio`} />}
        <div className="studio-card-cover-shade" />
        {showFavoriteButton && !isMyStudio && (
          <button
            className={`heart-button studio-card-favorite ${isFavorite ? "heart-active" : ""}`}
            type="button"
            aria-label={isFavorite ? "Remove studio from favorites" : "Add studio to favorites"}
            onClick={() => onToggleFavorite?.(studio)}
          >
            {isFavorite ? "♥" : "♡"}
          </button>
        )}
      </div>

      <div className="studio-card-identity">
        <div className="studio-card-logo">
          {logoUrl ? <img src={getImageUrl(logoUrl)} alt="" /> : <span>{initials(studio.name)}</span>}
        </div>
        {verified && <span className="studio-verified-badge">✓ Verified</span>}
      </div>

      <div className="studio-card-content">
        <h2>{studio.name}</h2>
        <p className="studio-card-location">⌖ {[studio.city, studio.country].filter(Boolean).join(", ")}</p>

        <div className="studio-card-stats">
          {reviewCount > 0 && rating != null && (
            <span className="studio-card-rating">★ <strong>{Number(rating).toFixed(1)}</strong> · {reviewCount} review{reviewCount === 1 ? "" : "s"}</span>
          )}
          <span>♟ <strong>{studio.artistCount ?? artists.length}</strong> artist{(studio.artistCount ?? artists.length) === 1 ? "" : "s"}</span>
        </div>

        {previews.length > 0 ? (
          <div className="studio-portfolio-carousel">
            {canLeft && <button className="studio-carousel-arrow left" type="button" onClick={() => move(-1)} aria-label="Previous portfolio images">‹</button>}
            <div className="studio-portfolio-track" ref={trackRef} onScroll={syncArrows}>
              {previews.map((url, index) => (
                <img className="studio-portfolio-slide" key={`${url}-${index}`} src={getImageUrl(url)} alt={`${studio.name} portfolio ${index + 1}`} />
              ))}
            </div>
            {canRight && <button className="studio-carousel-arrow right" type="button" onClick={() => move(1)} aria-label="Next portfolio images">›</button>}
          </div>
        ) : (
          <div className="studio-portfolio-empty-premium">Portfolio preview coming soon</div>
        )}

        {styles.length > 0 && (
          <div className="studio-style-strip" aria-label="Studio specialties">
            {styles.map((style) => <span key={style}>{style}</span>)}
          </div>
        )}

        <button className="primary-button studio-card-open" type="button" onClick={() => navigate(isMyStudio ? "/my-studio" : `/studios/${studio.id}`)}>
          {isMyStudio ? "My Studio" : "View studio"}
        </button>
      </div>
    </article>
  );
}

export default StudioCard;
