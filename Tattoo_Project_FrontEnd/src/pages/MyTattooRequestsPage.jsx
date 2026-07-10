import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyTattooRequests } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";
import { formatDate, formatDateTime, formatTime, getEntityId, getStatusClass, getStatusName } from "../utils/format";
import { getImageUrl } from "../utils/images";

const STATUS = {
  SUBMITTED: 0,
  WAITING_FOR_CONSULTATION: 3,
  CONSULTATION_COMPLETED: 4,
  TATTOO_BOOKED: 5,
  IN_PROGRESS: 6,
  COMPLETED: 7,
  REJECTED: 8,
};

function canBookConsultation(request) {
  return request.status === STATUS.WAITING_FOR_CONSULTATION && request.artistResponse && !request.consultation;
}

function canBookSession(request) {
  return (
    request.status === STATUS.CONSULTATION_COMPLETED ||
    request.status === STATUS.TATTOO_BOOKED ||
    request.status === STATUS.IN_PROGRESS
  ) && (request.remainingSessionsToBook === undefined || request.remainingSessionsToBook === null || request.remainingSessionsToBook > 0);
}

function MyTattooRequestsPage() {
  const [requests, setRequests] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadRequests();
  }, []);

  async function loadRequests() {
    try {
      const response = await getMyTattooRequests();
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setRequests(data || []);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">Bookings</p>
          <h1>Track your bookings</h1>
          <p>View statuses, artist responses, consultation details, tattoo sessions, and leave a review after completion.</p>
        </div>

        {isLoading && <p className="message">Loading tattoo requests...</p>}
        {error && <p className="error">{error}</p>}
        {!isLoading && !error && requests.length === 0 && <p className="message">You do not have any bookings yet.</p>}

        <div className="grid-2">
          {requests.map((request, index) => {
            const id = getEntityId(request, index);
            const isCompleted = request.status === STATUS.COMPLETED || request.status === "Completed";

            return (
              <article className="card artist-card" key={index}>
                <div className="card-head">
                  <div>
                    <p className="subtitle inline-subtitle">Tattoo Request</p>
                    <h2>{request.placement || "Tattoo request"}</h2>
                  </div>
                  <span className={`status-pill ${getStatusClass(request.status)}`}>{getStatusName(request.status)}</span>
                </div>

                <p className="muted">{request.description}</p>

                <div className="section highlighted">
                  <h3>Artist</h3>
                  <div className="info-list">
                    <p><span>Name:</span> {request.tattooArtistName || "Not provided"}</p>
                    <p><span>Studio:</span> {request.studioName || "Not provided"}</p>
                  </div>
                </div>

                <div className="info-list">
                  <p><span>Created:</span> {formatDate(request.createdOn)}</p>
                </div>

                {request.images?.length > 0 && (
                  <div className="section">
                    <h3>Reference images</h3>
                    <div className="image-grid">
                      {request.images.slice(0, 4).map((image, imageIndex) => (
                        <img key={imageIndex} src={getImageUrl(image.imageUrl)} alt="Reference" />
                      ))}
                    </div>
                  </div>
                )}

                {request.artistResponse && (
                  <div className="section highlighted">
                    <h3>Artist response</h3>
                    <p className="muted">{request.artistResponse.responseMessage}</p>
                    <div className="small-list-row">
                      <span>{request.artistResponse.estimatedPrice} BGN</span>
                      <span>{request.artistResponse.estimatedHours} hours</span>
                      <span>{formatDate(request.artistResponse.createdOn)}</span>
                    </div>
                  </div>
                )}

                {request.consultation && (
                  <div className="section">
                    <h3>Consultation</h3>
                    <div className="info-list">
                      <p><span>Start:</span> {formatDateTime(request.consultation.startTime)}</p>
                      <p><span>End:</span> {formatDateTime(request.consultation.endTime)}</p>
                      <p><span>Notes:</span> {request.consultation.notes || "No notes"}</p>
                    </div>
                  </div>
                )}

                {request.tattooSessions?.length > 0 && (
                  <div className="section">
                    <h3>Tattoo sessions</h3>
                    <div className="small-list">
                      {request.tattooSessions.map((session, sessionIndex) => (
                        <div className="small-list-row" key={sessionIndex}>
                          <span>Session {sessionIndex + 1}</span>
                          <span>{formatDateTime(session.startTime)} - {formatTime(session.endTime)}</span>
                          <span>{session.priceForTheSession} BGN</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                <div className="action-row">
                  {canBookConsultation(request) && (
                    <Link className="secondary-button" to={`/book-consultation/${id}`}>Book consultation</Link>
                  )}
                  {canBookSession(request) && (
                    <Link className="primary-button" to={`/book-session/${id}`}>Book session</Link>
                  )}
                  {isCompleted && <Link className="secondary-button" to={`/review/${id}`}>Leave review</Link>}
                  {!canBookConsultation(request) && !canBookSession(request) && !isCompleted && (
                    <p className="muted">No booking action is available for this status.</p>
                  )}
                </div>
              </article>
            );
          })}
        </div>
      </section>
    </main>
  );
}

export default MyTattooRequestsPage;
