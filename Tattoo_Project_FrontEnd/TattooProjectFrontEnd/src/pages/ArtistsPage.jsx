
import { useEffect, useState } from "react";
import { getAllArtists } from "../api/artistApi";
import "../styles/artists.css";

function ArtistsPage() {
  const [artists, setArtists] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadArtists() {
      try {
        const response = await getAllArtists();

        if (!response.ok) {
          const errorText = await response.text();
          setError(errorText || "Failed to load tattoo artists.");
          return;
        }

        const data = await response.json();
        setArtists(data);
      } catch {
        setError("Server connection failed. Please try again.");
      } finally {
        setIsLoading(false);
      }
    }

    loadArtists();
  }, []);

  return (
    <main className="artists-page">
      <section className="artists-container">
        <div className="artists-header">
          <p className="artists-subtitle">Tattoo Artists</p>
          <h1>Find your tattoo artist</h1>
          <p>
            Browse tattoo artists, check their studio information, requirements,
            portfolio images, and working schedule.
          </p>
        </div>

        {isLoading && <p className="artists-message">Loading artists...</p>}

        {error && <p className="artists-error">{error}</p>}

        {!isLoading && !error && artists.length === 0 && (
          <p className="artists-message">No tattoo artists found.</p>
        )}

        <div className="artists-grid">
          {artists.map((artist, index) => (
            <article className="artist-card" key={index}>
              <div className="artist-card-header">
                <div>
                  <h2>{artist.studioName}</h2>
                  <p className="artist-name">
                    {artist.firstName} {artist.lastName}
                  </p>
                </div>

                {artist.isVerified && (
                  <span className="verified-badge">Verified</span>
                )}
              </div>

              <p className="artist-description">{artist.description}</p>

              <div className="artist-info-list">
                <p>
                  <span>Studio:</span> {artist.studioAddress}
                </p>
                <p>
                  <span>Phone:</span> {artist.phoneNumber}
                </p>
                <p>
                  <span>Email:</span> {artist.email}
                </p>
                <p>
                  <span>Online consultation:</span>{" "}
                  {artist.offersOnlineConsultation ? "Yes" : "No"}
                </p>
                <p>
                  <span>Deposit:</span>{" "}
                  {artist.requiresDeposit
                    ? `${artist.depositAmount} BGN`
                    : "Not required"}
                </p>
                <p>
                  <span>Consultation duration:</span>{" "}
                  {artist.consultationDurationMinutes} minutes
                </p>
              </div>

              {artist.requirements && artist.requirements.length > 0 && (
                <div className="artist-section">
                  <h3>Requirements</h3>
                  <ul>
                    {artist.requirements.map((requirement, requirementIndex) => (
                      <li key={requirementIndex}>
                        {requirement.description}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {artist.schedules && artist.schedules.length > 0 && (
                <div className="artist-section">
                  <h3>Schedule</h3>

                  <div className="schedule-list">
                    {artist.schedules.map((schedule, scheduleIndex) => (
                      <div className="schedule-item" key={scheduleIndex}>
                        <span>{getDayName(schedule.dayOfWeek)}</span>
                        <span>
                          {formatTime(schedule.startTime)} -{" "}
                          {formatTime(schedule.endTime)}
                        </span>
                        <span className="schedule-type">
                          {getScheduleTypeName(schedule.scheduleType)}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {artist.portfolioImages && artist.portfolioImages.length > 0 && (
                <div className="artist-section">
                  <h3>Portfolio</h3>

                  <div className="portfolio-grid">
                    {artist.portfolioImages.slice(0, 4).map((image, imageIndex) => (
                      <img
                        key={imageIndex}
                        src={image.imageUrl}
                        alt={`${artist.studioName} portfolio ${imageIndex + 1}`}
                      />
                    ))}
                  </div>
                </div>
              )}

              <button className="artist-action-button">
                Create Tattoo Request
              </button>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

function getDayName(dayOfWeek) {
  const days = {
    0: "Sunday",
    1: "Monday",
    2: "Tuesday",
    3: "Wednesday",
    4: "Thursday",
    5: "Friday",
    6: "Saturday",
  };

  return days[dayOfWeek] || "Unknown day";
}

function getScheduleTypeName(scheduleType) {
  if (scheduleType === 1) {
    return "Consultation";
  }

  if (scheduleType === 0) {
    return "Tattoo session";
  }

  return "Schedule";
}

function formatTime(time) {
  if (!time) {
    return "";
  }

  return time.slice(0, 5);
}

export default ArtistsPage;