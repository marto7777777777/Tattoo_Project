import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyTattooRequests } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";
import { formatAppointmentRange, formatDate, getEntityId, getStatusClass, getStatusName } from "../utils/format";
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

function getRemainingSessions(request) {
  const value = request?.remainingSessionsToBook ?? request?.RemainingSessionsToBook;
  if (value === null || value === undefined || value === "") return null;

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function getPlannedSessions(request) {
  const prices = request?.priceForSession ?? request?.PriceForSession;
  const durations =
    request?.durationHoursForSession ?? request?.DurationHoursForSession;

  if (!Array.isArray(prices) || !Array.isArray(durations)) return [];

  return durations
    .slice(0, Math.min(prices.length, durations.length))
    .map((durationHours, index) => ({
      durationHours,
      price: prices[index],
    }));
}

function MyTattooRequestsPage() {
  const [requests, setRequests] = useState([]);
  const [selectedRequest, setSelectedRequest] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  const [error, setError] = useState("");
  const [previewImage, setPreviewImage] = useState(null);
  const [viewMode, setViewMode] = useState("active");
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
      setRequests([...(data || [])].sort((a, b) => {
        const statusA = typeof a.status === "number" ? a.status : 0;
        const statusB = typeof b.status === "number" ? b.status : 0;
        if (statusA !== statusB) return statusB - statusA;
        return new Date(b.createdOn || 0) - new Date(a.createdOn || 0);
      }));
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
    const remaining = getRemainingSessions(request);

    if (canBookConsultation(request)) {
      return <Link className="primary-button" to={`/book-consultation/${id}`}>Book consultation</Link>;
    }

    if (isSessionWorkflow(request)) {
      if (remaining != null && remaining > 0) {
        return <Link className="primary-button" to={`/book-session/${id}`}>Book tattoo session</Link>;
      }

      return (
        <button
          className="secondary-button disabled-action-button"
          type="button"
          disabled
          title="All planned tattoo sessions have already been booked."
        >
          No more sessions
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

        <div className="filter-tabs client-project-tabs">
          <button type="button" className={`filter-tab ${viewMode === "active" ? "filter-tab-active" : ""}`} onClick={() => setViewMode("active")}>Active projects <span>{requests.filter((r) => r.status !== STATUS.COMPLETED && r.status !== "Completed").length}</span></button>
          <button type="button" className={`filter-tab ${viewMode === "completed" ? "filter-tab-active" : ""}`} onClick={() => setViewMode("completed")}>Completed tattoos <span>{requests.filter((r) => r.status === STATUS.COMPLETED || r.status === "Completed").length}</span></button>
        </div>

        <div className="request-card-list">
          {requests.filter((request) => viewMode === "completed" ? (request.status === STATUS.COMPLETED || request.status === "Completed") : (request.status !== STATUS.COMPLETED && request.status !== "Completed")).map((request, index) => (
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

      {previewImage && (
        <div className="modal-backdrop image-lightbox" onClick={() => setPreviewImage(null)}>
          <button className="lightbox-close" type="button" onClick={() => setPreviewImage(null)}>×</button>
          <img src={previewImage} alt="Tattoo reference enlarged" onClick={(event) => event.stopPropagation()} />
        </div>
      )}

      {selectedRequest && (
        <div className="modal-backdrop" onClick={() => setSelectedRequest(null)}>
          <section className="modal-card request-modal structured-request-modal request-project-modal" onClick={(event) => event.stopPropagation()}>
            <header className="request-project-hero">
              <div className="request-project-hero-top">
                <span className={`status-pill ${getStatusClass(selectedRequest.status)}`}>
                  {getStatusName(selectedRequest.status)}
                </span>
                <button className="icon-button request-project-close" type="button" onClick={() => setSelectedRequest(null)} aria-label="Close request">×</button>
              </div>
              <p className="request-project-kicker">Tattoo project</p>
              <h2>{selectedRequest.placement || "Tattoo request"}</h2>
              <p className="request-project-description">{selectedRequest.description}</p>
              <div className="request-project-meta">
                <div><span>Artist</span><strong>{selectedRequest.tattooArtistName || "Not provided"}</strong></div>
                <div><span>Studio</span><strong>{selectedRequest.studioName || "Not provided"}</strong></div>
                <div><span>Style</span><strong>{selectedRequest.tattooStyle || "Not provided"}</strong></div>
                <div><span>Created</span><strong>{formatDate(selectedRequest.createdOn)}</strong></div>
              </div>
            </header>

            <nav className="request-detail-tabs request-project-tabs" aria-label="Request sections">
              {[
                { value: "overview", icon: "⌂", label: "Overview" },
                { value: "progress", icon: "✓", label: "Progress" },
                { value: "appointments", icon: "◷", label: "Appointments" },
                { value: "images", icon: "◇", label: "Images" },
              ].map((tab) => (
                <button key={tab.value} type="button" className={`request-detail-tab ${activeTab === tab.value ? "request-detail-tab-active" : ""}`} onClick={() => setActiveTab(tab.value)}>
                  <span aria-hidden="true">{tab.icon}</span>
                  {tab.label}
                </button>
              ))}
            </nav>

            {activeTab === "overview" && (
              <div className="request-detail-panel request-project-content">
                <div className="request-project-overview-grid">
                  <section className="request-project-brief">
                    <p className="request-project-section-label">Your brief</p>
                    <h3>Project direction</h3>
                    <p>{selectedRequest.description}</p>
                    <div className="request-brief-facts">
                      <div><span>Placement</span><strong>{selectedRequest.placement || "Not provided"}</strong></div>
                      <div><span>Tattoo style</span><strong>{selectedRequest.tattooStyle || "Not provided"}</strong></div>
                    </div>
                  </section>

                  <aside className="request-project-side">
                    {selectedRequest.artistResponse ? (
                      <section className="request-artist-note">
                        <p className="request-project-section-label">Artist response</p>
                        <blockquote>{selectedRequest.artistResponse.responseMessage || "No message was added."}</blockquote>
                        <div className="request-estimate-grid">
                          <div><span>Indicative price</span><strong>{selectedRequest.artistResponse.estimatedPrice} BGN</strong></div>
                          <div><span>Indicative time</span><strong>{selectedRequest.artistResponse.estimatedHours} hours</strong></div>
                        </div>
                      </section>
                    ) : (
                      <section className="request-project-empty">
                        <span>◌</span>
                        <div><strong>Waiting for artist</strong><p>The response will appear here.</p></div>
                      </section>
                    )}
                  </aside>
                </div>

                {getPlannedSessions(selectedRequest).length > 0 && (
                  <section className="request-session-plan">
                    <div className="request-session-plan-head">
                      <div>
                        <p className="request-project-section-label">Confirmed after consultation</p>
                        <h3>Your session plan</h3>
                      </div>
                      <span>{getPlannedSessions(selectedRequest).length} sessions</span>
                    </div>
                    <div className="request-session-plan-grid">
                      {getPlannedSessions(selectedRequest).map((session, index) => (
                        <article className="request-session-ticket" key={index}>
                          <div className="request-session-number">{String(index + 1).padStart(2, "0")}</div>
                          <div><span>Session {index + 1}</span><strong>{session.durationHours} hours</strong></div>
                          <div><span>Price</span><strong>{session.price} BGN</strong></div>
                        </article>
                      ))}
                    </div>
                  </section>
                )}

                <section className="request-next-step">
                  <div>
                    <p className="request-project-section-label">Next step</p>
                    <h3>Continue your project</h3>
                  </div>
                  <div className="modal-next-action">{renderNextAction(selectedRequest)}</div>
                </section>
              </div>
            )}

            {activeTab === "progress" && <div className="request-detail-panel request-project-content"><section className="request-project-tab-card"><p className="request-project-section-label">Project journey</p><h3>Where your tattoo stands</h3><RequestWorkflowTimeline request={selectedRequest} /></section></div>}

            {activeTab === "appointments" && (
              <div className="request-detail-panel request-project-content request-appointments-grid">
                {selectedRequest.consultation ? (
                  <div className="detail-section-card consultation-accent-card">
                    <h3>Consultation</h3>
                    <p>{formatAppointmentRange(
                      selectedRequest.consultation.startTime,
                      selectedRequest.consultation.endTime,
                      selectedRequest.consultation.durationMinutes
                        ?? selectedRequest.consultation.consultationDurationMinutes
                        ?? selectedRequest.consultationDurationMinutes
                        ?? selectedRequest.artist?.consultationDurationMinutes
                        ?? 30,
                    )}</p>
                    <p className="muted">{selectedRequest.consultation.notes || "No notes"}</p>
                  </div>
                ) : <div className="request-project-empty"><span>◷</span><div><strong>No consultation booked</strong><p>Your confirmed consultation will appear here.</p></div></div>}

                {selectedRequest.tattooSessions?.length > 0 ? (
                  <div className="detail-section-card session-accent-card">
                    <h3>Tattoo sessions</h3>
                    <div className="small-list">
                      {selectedRequest.tattooSessions.map((session, index) => (
                        <article className="request-appointment-session" key={index}>
                          <strong className="request-appointment-session-title">Session {index + 1}</strong>
                          <span className="request-appointment-session-time">{formatAppointmentRange(
                            session.startTime,
                            session.endTime,
                            session.durationHours
                              ?? session.durationInHours
                              ?? getPlannedSessions(selectedRequest)[index]?.durationHours,
                            "hours",
                          )}</span>
                          <span className="request-appointment-session-price">
                            <small>Price</small>
                            <strong>{session.priceForTheSession} BGN</strong>
                          </span>
                        </article>
                      ))}
                    </div>
                  </div>
                ) : <div className="request-project-empty"><span>◇</span><div><strong>No tattoo sessions booked</strong><p>Booked sessions will appear here.</p></div></div>}
              </div>
            )}

            {activeTab === "images" && (
              <div className="request-detail-panel request-project-content">
                {selectedRequest.images?.length > 0 ? (
                  <div className="image-grid request-image-grid request-project-gallery">
                    {selectedRequest.images.map((image, index) => (
                      <button className="request-detail-image-button" type="button" key={index} onClick={() => setPreviewImage(getImageUrl(image.imageUrl))}>
                        <img src={getImageUrl(image.imageUrl)} alt={`Reference ${index + 1}`} />
                      </button>
                    ))}
                  </div>
                ) : <div className="request-project-empty"><span>◇</span><div><strong>No reference images</strong><p>This project was created without uploaded images.</p></div></div>}
              </div>
            )}
          </section>
        </div>
      )}
    </main>
  );
}

export default MyTattooRequestsPage;
