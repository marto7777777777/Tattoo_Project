
import { useEffect, useState } from "react";
import { getMyTattooRequests } from "../api/tattooRequestApi";
import "../styles/myTattooRequests.css";

function MyTattooRequestsPage() {
  const [requests, setRequests] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function loadRequests() {
      try {
        const response = await getMyTattooRequests();

        if (!response.ok) {
          const errorText = await response.text();
          setError(errorText || "Failed to load tattoo requests.");
          return;
        }

        const data = await response.json();
        setRequests(data);
      } catch {
        setError("Server connection failed. Please try again.");
      } finally {
        setIsLoading(false);
      }
    }

    loadRequests();
  }, []);

  return (
    <main className="my-requests-page">
      <section className="my-requests-container">
        <div className="my-requests-header">
          <p className="my-requests-subtitle">My Tattoo Requests</p>
          <h1>Track your tattoo ideas</h1>
          <p>
            View your submitted tattoo requests, their current status, reference
            images, consultations, artist responses, and booked sessions.
          </p>
        </div>

        {isLoading && (
          <p className="my-requests-message">Loading tattoo requests...</p>
        )}

        {error && <p className="my-requests-error">{error}</p>}

        {!isLoading && !error && requests.length === 0 && (
          <p className="my-requests-message">
            You have not created any tattoo requests yet.
          </p>
        )}

        <div className="my-requests-grid">
          {requests.map((request, index) => (
            <article className="request-card" key={index}>
              <div className="request-card-header">
                <div>
                  <p className="request-label">Request #{index + 1}</p>
                  <h2>{request.placement || "Tattoo placement"}</h2>
                </div>

                <span className={`request-status ${getStatusClass(request.status)}`}>
                  {getStatusName(request.status)}
                </span>
              </div>

              <p className="request-description">{request.description}</p>

              <div className="request-info-list">
                <p>
                  <span>Placement:</span> {request.placement || "Not specified"}
                </p>

                <p>
                  <span>Created on:</span> {formatDate(request.createdOn)}
                </p>

                <p>
                  <span>Artist ID:</span> {request.tattooArtistId}
                </p>
              </div>

              {request.images && request.images.length > 0 && (
                <div className="request-section">
                  <h3>Reference images</h3>

                  <div className="request-images-grid">
                    {request.images.slice(0, 4).map((image, imageIndex) => (
                      <img
                        key={imageIndex}
                        src={image.imageUrl}
                        alt={`Tattoo reference ${imageIndex + 1}`}
                      />
                    ))}
                  </div>
                </div>
              )}

              {request.artistResponse && (
                <div className="request-section highlighted-section">
                  <h3>Artist response</h3>

                  <p>{request.artistResponse.responseMessage}</p>

                  <div className="request-mini-grid">
                    <span>
                      Estimated price: {request.artistResponse.estimatedPrice} BGN
                    </span>
                    <span>
                      Estimated hours: {request.artistResponse.estimatedHours}
                    </span>
                  </div>
                </div>
              )}

              {request.consultation && (
                <div className="request-section">
                  <h3>Consultation</h3>

                  <div className="request-info-list">
                    <p>
                      <span>Start:</span>{" "}
                      {formatDateTime(request.consultation.startTime)}
                    </p>
                    <p>
                      <span>End:</span>{" "}
                      {formatDateTime(request.consultation.endTime)}
                    </p>
                    <p>
                      <span>Notes:</span>{" "}
                      {request.consultation.notes || "No notes"}
                    </p>
                  </div>
                </div>
              )}

              {request.tattooSessions && request.tattooSessions.length > 0 && (
                <div className="request-section">
                  <h3>Tattoo sessions</h3>

                  <div className="sessions-list">
                    {request.tattooSessions.map((session, sessionIndex) => (
                      <div className="session-item" key={sessionIndex}>
                        <span>Session {sessionIndex + 1}</span>
                        <span>
                          {formatDateTime(session.startTime)} -{" "}
                          {formatTime(session.endTime)}
                        </span>
                        <span>{session.priceForTheSession} BGN</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="request-actions">
                {request.status === 1 && (
                  <button className="request-action-button">
                    Book Consultation
                  </button>
                )}

                {request.status === 2 && (
                  <button className="request-action-button">
                    Book Tattoo Session
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

function getStatusName(status) {
  const statuses = {
    0: "Submitted",
    1: "Waiting for consultation",
    2: "Consultation completed",
    3: "Tattoo booked",
    4: "In progress",
    5: "Completed",
    6: "Rejected",
  };

  return statuses[status] || "Unknown";
}

function getStatusClass(status) {
  const classes = {
    0: "status-submitted",
    1: "status-waiting",
    2: "status-consultation-completed",
    3: "status-booked",
    4: "status-progress",
    5: "status-completed",
    6: "status-rejected",
  };

  return classes[status] || "";
}

function formatDate(dateValue) {
  if (!dateValue) {
    return "Unknown date";
  }

  return new Date(dateValue).toLocaleDateString();
}

function formatDateTime(dateValue) {
  if (!dateValue) {
    return "";
  }

  return new Date(dateValue).toLocaleString();
}

function formatTime(dateValue) {
  if (!dateValue) {
    return "";
  }

  return new Date(dateValue).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default MyTattooRequestsPage;