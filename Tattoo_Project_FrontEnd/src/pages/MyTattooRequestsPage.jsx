import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyTattooRequests } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";
import { formatDate, formatDateTime, formatTime, getEntityId, getStatusClass, getStatusName } from "../utils/format";
import { getImageUrl } from "../utils/images";
import RequestWorkflowTimeline from "../components/RequestWorkflowTimeline";

const STATUS = {
  WAITING_FOR_CONSULTATION: 3,
  CONSULTATION_COMPLETED: 4,
  TATTOO_BOOKED: 5,
  IN_PROGRESS: 6,
  COMPLETED: 7,
};

function canBookConsultation(request) {
  return request.status === STATUS.WAITING_FOR_CONSULTATION && request.artistResponse && !request.consultation;
}

function isSessionWorkflow(request) {
  return [STATUS.CONSULTATION_COMPLETED, STATUS.TATTOO_BOOKED, STATUS.IN_PROGRESS].includes(request.status);
}

function MyTattooRequestsPage() {
  const [requests, setRequests] = useState([]);
  const [selectedRequest, setSelectedRequest] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => { loadRequests(); }, []);

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

  function openRequest(request) {
    setSelectedRequest(request);
    setActiveTab("overview");
  }

  function renderNextAction(request) {
    const id = getEntityId(request);
    const isCompleted = request.status === STATUS.COMPLETED || request.status === "Completed";
    const remaining = request.remainingSessionsToBook;

    if (canBookConsultation(request)) {
      return <Link className="primary-button" to={`/book-consultation/${id}`}>Book consultation</Link>;
    }

    if (isSessionWorkflow(request)) {
      if (remaining == null || remaining > 0) {
        return <Link className="primary-button" to={`/book-session/${id}`}>Book tattoo session</Link>;
      }

      return (
        <button
          className="secondary-button disabled-action-button"
          type="button"
          disabled
          title="All planned tattoo sessions have already been booked."
        >
          No sessions available
        </button>
      );
    }

    if (isCompleted) {
      return <Link className="secondary-button" to={`/review/${id}`}>Leave review</Link>;
    }

    return <p className="muted next-action-note">There is no action required from you right now.</p>;
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">Bookings</p>
          <h1>Your tattoo projects</h1>
          <p>Each request shows one clear next step. Open it for progress, appointments, and reference images.</p>
        </div>

        {isLoading && <p className="message">Loading tattoo requests...</p>}
        {error && <p className="error">{error}</p>}
        {!isLoading && !error && requests.length === 0 && <p className="message">You do not have any bookings yet.</p>}

        <div className="request-card-list">
          {requests.map((request, index) => (
            <article className="card structured-request-card" key={getEntityId(request, index)}>
              <div className="request-card-main">
                <div className="card-head">
                  <div>
                    <p className="subtitle inline-subtitle">{request.tattooArtistName || "Tattoo artist"}</p>
                    <h2>{request.placement || "Tattoo request"}</h2>
                    <p className="muted studio-line">{request.studioName || "Studio not provided"}</p>
                  </div>
                  <span className={`status-pill ${getStatusClass(request.status)}`}>{getStatusName(request.status)}</span>
                </div>
                <p className="muted clamp-text">{request.description}</p>
                <RequestWorkflowTimeline request={request} compact />
              </div>

              <aside className="request-card-action">
                <span className="eyebrow-label">Next action</span>
                {renderNextAction(request)}
                <button className="secondary-button" type="button" onClick={() => openRequest(request)}>Open request</button>
              </aside>
            </article>
          ))}
        </div>
      </section>

      {selectedRequest && (
        <div className="modal-backdrop" onClick={() => setSelectedRequest(null)}>
          <section className="modal-card request-modal structured-request-modal" onClick={(event) => event.stopPropagation()}>
            <div className="modal-head">
              <div>
                <p className="subtitle">{selectedRequest.tattooArtistName || "Tattoo artist"} · {selectedRequest.studioName || "Studio"}</p>
                <h2>{selectedRequest.placement || "Tattoo request"}</h2>
              </div>
              <button className="icon-button" type="button" onClick={() => setSelectedRequest(null)}>×</button>
            </div>

            <div className="request-detail-tabs">
              {["overview", "progress", "appointments", "images"].map((tab) => (
                <button key={tab} type="button" className={`request-detail-tab ${activeTab === tab ? "request-detail-tab-active" : ""}`} onClick={() => setActiveTab(tab)}>
                  {tab.charAt(0).toUpperCase() + tab.slice(1)}
                </button>
              ))}
            </div>

            {activeTab === "overview" && (
              <div className="request-detail-panel">
                <div className="detail-section-card">
                  <h3>Request overview</h3>
                  <p className="muted">{selectedRequest.description}</p>
                  <div className="info-list">
                    <p><span>Artist:</span> {selectedRequest.tattooArtistName || "Not provided"}</p>
                    <p><span>Studio:</span> {selectedRequest.studioName || "Not provided"}</p>
                    <p><span>Placement:</span> {selectedRequest.placement || "Not provided"}</p>
                    <p><span>Style:</span> {selectedRequest.tattooStyle || "Not provided"}</p>
                    <p><span>Created:</span> {formatDate(selectedRequest.createdOn)}</p>
                  </div>
                </div>
                {selectedRequest.artistResponse && (
                  <div className="detail-section-card highlighted">
                    <h3>Artist response</h3>
                    <p className="muted">{selectedRequest.artistResponse.responseMessage}</p>
                    <div className="info-list">
                      <p><span>Estimated price:</span> {selectedRequest.artistResponse.estimatedPrice} BGN</p>
                      <p><span>Estimated time:</span> {selectedRequest.artistResponse.estimatedHours} hours</p>
                    </div>
                  </div>
                )}
                <div className="modal-next-action">{renderNextAction(selectedRequest)}</div>
              </div>
            )}

            {activeTab === "progress" && <div className="request-detail-panel"><RequestWorkflowTimeline request={selectedRequest} /></div>}

            {activeTab === "appointments" && (
              <div className="request-detail-panel">
                {selectedRequest.consultation ? (
                  <div className="detail-section-card consultation-accent-card">
                    <h3>Consultation</h3>
                    <p>{formatDateTime(selectedRequest.consultation.startTime)} – {formatTime(selectedRequest.consultation.endTime)}</p>
                    <p className="muted">{selectedRequest.consultation.notes || "No notes"}</p>
                  </div>
                ) : <p className="message">No consultation booked yet.</p>}

                {selectedRequest.tattooSessions?.length > 0 ? (
                  <div className="detail-section-card session-accent-card">
                    <h3>Tattoo sessions</h3>
                    <div className="small-list">
                      {selectedRequest.tattooSessions.map((session, index) => (
                        <div className="small-list-row" key={index}>
                          <span>Session {index + 1}</span>
                          <span>{formatDateTime(session.startTime)} – {formatTime(session.endTime)}</span>
                          <span>{session.priceForTheSession} BGN</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : <p className="message">No tattoo sessions booked yet.</p>}
              </div>
            )}

            {activeTab === "images" && (
              <div className="request-detail-panel">
                {selectedRequest.images?.length > 0 ? (
                  <div className="image-grid request-image-grid">
                    {selectedRequest.images.map((image, index) => (
                      <a href={getImageUrl(image.imageUrl)} target="_blank" rel="noreferrer" key={index}>
                        <img src={getImageUrl(image.imageUrl)} alt={`Reference ${index + 1}`} />
                      </a>
                    ))}
                  </div>
                ) : <p className="message">No reference images were uploaded.</p>}
              </div>
            )}
          </section>
        </div>
      )}
    </main>
  );
}

export default MyTattooRequestsPage;
