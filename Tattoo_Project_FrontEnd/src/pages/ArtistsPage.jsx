import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getAllArtists } from "../api/artistApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { formatTime, getDayName, getEntityId, getScheduleTypeName } from "../utils/format";

function ArtistsPage() {
  const navigate = useNavigate();
  const { isLoggedIn, isClient } = useAuth();
  const [artists, setArtists] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => { loadArtists(); }, []);

  async function loadArtists() {
    try {
      const response = await getAllArtists();
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : JSON.stringify(data)); return; }
      setArtists(data);
    } catch { setError("Server connection failed. Please try again."); }
    finally { setIsLoading(false); }
  }

  function handleCreateRequest(artist, index) {
    const artistId = getEntityId(artist, index);
    localStorage.setItem("selectedArtist", JSON.stringify({ ...artist, id: artistId }));
    if (!isLoggedIn) navigate("/login");
    else if (!isClient) navigate("/create-client-profile");
    else navigate(`/create-tattoo-request/${artistId}`);
  }

  return (
    <main className="page-shell"><section className="container"><div className="header"><p className="subtitle">Tattoo Artists</p><h1>Find your tattoo artist</h1><p>Browse studios, portfolio images, requirements, consultation settings, and schedule blocks.</p></div>
      {isLoading && <p className="message">Loading artists...</p>}{error && <p className="error">{error}</p>}{!isLoading && !error && artists.length === 0 && <p className="message">No tattoo artists found.</p>}
      <div className="grid-2">{artists.map((artist,index)=><article className="card artist-card" key={index}><div className="card-head"><div><h2>{artist.studioName}</h2><p className="subtitle inline-subtitle">{artist.firstName} {artist.lastName}</p></div>{artist.isVerified && <span className="status-pill status-completed">Verified</span>}</div><p className="muted">{artist.description}</p><div className="info-list"><p><span>Address:</span> {artist.studioAddress}</p><p><span>Phone:</span> {artist.phoneNumber}</p><p><span>Online consultation:</span> {artist.offersOnlineConsultation ? "Yes" : "No"}</p><p><span>Deposit:</span> {artist.requiresDeposit ? `${artist.depositAmount} BGN` : "Not required"}</p><p><span>Consultation duration:</span> {artist.consultationDurationMinutes} minutes</p></div>{artist.requirements?.length > 0 && <div className="section"><h3>Requirements</h3><ul className="muted">{artist.requirements.map((r,i)=><li key={i}>{r.description}</li>)}</ul></div>}{artist.schedules?.length > 0 && <div className="section"><h3>Schedule</h3><div className="small-list">{artist.schedules.map((s,i)=><div className="small-list-row" key={i}><span>{getDayName(s.dayOfWeek)}</span><span>{formatTime(s.startTime)} - {formatTime(s.endTime)}</span><span className="status-pill">{getScheduleTypeName(s.scheduleType)}</span></div>)}</div></div>}{artist.portfolioImages?.length > 0 && <div className="section"><h3>Portfolio</h3><div className="image-grid">{artist.portfolioImages.slice(0,4).map((img,i)=><img key={i} src={img.imageUrl} alt="Portfolio" />)}</div></div>}<button className="primary-button full-button" onClick={()=>handleCreateRequest(artist,index)}>Create Tattoo Request</button></article>)}</div>
      <p className="muted backend-note">Note: if artist IDs are missing from backend DTOs, this page uses a fallback index. Add Id to GetTattooArtistDto for fully reliable click-through.</p>
    </section></main>
  );
}
export default ArtistsPage;
