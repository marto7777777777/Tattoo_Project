import { useNavigate } from "react-router-dom";
import UserAvatar from "./UserAvatar";
import { getImageUrl } from "../utils/images";

function StudioCard({ studio, isMyStudio = false }) {
  const navigate = useNavigate();
  const artists = studio?.artists || studio?.Artists || [];
  const previews = studio?.portfolioPreviewUrls || studio?.PortfolioPreviewUrls || [];

  return (
    <article className="studio-card">
      <div className="studio-card-topline">
        <div>
          <p className="subtitle inline-subtitle">Tattoo studio</p>
          <h2>{studio.name}</h2>
          <p className="muted">{[studio.city, studio.country].filter(Boolean).join(", ")}</p>
        </div>
        <span className="studio-artist-count">{studio.artistCount ?? artists.length} artist{(studio.artistCount ?? artists.length) === 1 ? "" : "s"}</span>
      </div>

      <p className="studio-card-description">{studio.description}</p>

      <div className="studio-artist-preview-row">
        {artists.slice(0, 4).map((artist) => (
          <div className="studio-artist-preview" key={artist.id} title={`${artist.firstName} ${artist.lastName}`}>
            <UserAvatar
              firstName={artist.firstName}
              lastName={artist.lastName}
              imageUrl={artist.profileImageUrl}
              size="small"
            />
            <span>{artist.firstName}</span>
          </div>
        ))}
      </div>

      <div className="studio-portfolio-preview">
        {previews.slice(0, 3).map((url, index) => (
          <img key={`${url}-${index}`} src={getImageUrl(url)} alt={`${studio.name} portfolio ${index + 1}`} />
        ))}
        {previews.length === 0 && <div className="studio-portfolio-empty">Portfolio preview coming soon</div>}
      </div>

      <button className="primary-button" type="button" onClick={() => navigate(isMyStudio ? "/my-studio" : `/studios/${studio.id}`)}>
        {isMyStudio ? "My Studio" : "View studio"} <span>→</span>
      </button>
    </article>
  );
}

export default StudioCard;
