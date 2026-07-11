import { useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import { createArtistResponse, rejectTattooRequest } from "../api/artistResponseApi";
import { completeConsultation, rejectConsultation } from "../api/consultationApi";
import { getMyArtistTattooRequests } from "../api/tattooRequestApi";
import { addMoreSessions, completeTattoo } from "../api/tattooSessionApi";
import { readResponse } from "../api/http";
import {
  formatDate,
  formatDateTime,
  formatTime,
  getStatusClass,
  getStatusName,
} from "../utils/format";
import { getImageUrl } from "../utils/images";
import RequestWorkflowTimeline from "../components/RequestWorkflowTimeline";

const STATUS = {
  SUBMITTED: 0,
  UNDER_REVIEW: 1,
  APPROVED: 2,
  WAITING_FOR_CONSULTATION: 3,
  CONSULTATION_COMPLETED: 4,
  TATTOO_BOOKED: 5,
  IN_PROGRESS: 6,
  COMPLETED: 7,
  REJECTED: 8,
};

const REQUEST_FILTERS = [
  { label: "All", value: "all" },
  { label: "New requests", value: "new" },
  { label: "Responded", value: "responded" },
  { label: "Consultation completed", value: "consultation-completed" },
  { label: "Tattoo active", value: "tattoo-active" },
  { label: "Completed projects", value: "completed" },
  { label: "Rejected", value: "rejected" },
];

function createEmptySession() {
  return { price: "", durationHours: "" };
}

function getRequestTitle(request) {
  return request.placement || "Tattoo request";
}

function getWorkflowStep(request) {
  if (request.status === STATUS.REJECTED) return "Rejected";
  if (request.status === STATUS.COMPLETED) return "Completed project";
  if (!request.artistResponse || request.status === STATUS.SUBMITTED) return "Needs artist response";
  if (request.status === STATUS.WAITING_FOR_CONSULTATION && !request.consultation) {
    return "Response sent · waiting for client to book consultation";
  }
  if (request.status === STATUS.WAITING_FOR_CONSULTATION && request.consultation) {
    return "Consultation booked";
  }
  if (request.status === STATUS.CONSULTATION_COMPLETED && !request.tattooSessions?.length) {
    return "Consultation completed · waiting for tattoo session booking";
  }
  if (request.status === STATUS.CONSULTATION_COMPLETED) return "Consultation completed";
  if (request.status === STATUS.TATTOO_BOOKED || request.status === STATUS.IN_PROGRESS) {
    return "Tattoo sessions active";
  }

  return getStatusName(request.status);
}

function getUpcomingConsultation(request) {
  if (!request.consultation) return null;

  const start = new Date(request.consultation.startTime);
  if (start < new Date()) return null;

  return request.consultation;
}

function getUpcomingTattooSession(request) {
  if (!request.tattooSessions?.length) return null;

  const now = new Date();

  return [...request.tattooSessions]
    .filter((session) => new Date(session.startTime) >= now)
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))[0] || null;
}

function matchesFilter(request, filter) {
  if (filter === "all") return true;
  if (filter === "new") return request.status === STATUS.SUBMITTED;
  if (filter === "responded") return request.status === STATUS.WAITING_FOR_CONSULTATION;
  if (filter === "consultation-completed") return request.status === STATUS.CONSULTATION_COMPLETED;
  if (filter === "tattoo-active") {
    return request.status === STATUS.TATTOO_BOOKED || request.status === STATUS.IN_PROGRESS;
  }
  if (filter === "completed") return request.status === STATUS.COMPLETED;
  if (filter === "rejected") return request.status === STATUS.REJECTED;

  return true;
}

function getFilterCount(requests, filter) {
  return requests.filter((request) => matchesFilter(request, filter)).length;
}

function ArtistRequestsPage() {
  const location = useLocation();
  const [requests, setRequests] = useState([]);
  const [selectedRequest, setSelectedRequest] = useState(null);
  const [activeFilter, setActiveFilter] = useState("all");
  const [activeAction, setActiveAction] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [responseForm, setResponseForm] = useState({
    estimatedPrice: "",
    estimatedHours: "",
    responseMessage: "",
  });

  const [sessions, setSessions] = useState([createEmptySession()]);
  const [extraSessions, setExtraSessions] = useState([createEmptySession()]);

  useEffect(() => {
    loadRequests();
  }, []);

  useEffect(() => {
    const params = new URLSearchParams(location.search);
    const requestId = Number(params.get("requestId"));

    if (!requestId || requests.length === 0) return;

    const request = requests.find((item) => item.id === requestId);
    if (request) {
      openRequest(request);
    }
  }, [location.search, requests]);

  const filteredRequests = useMemo(() => {
    return requests
      .filter((request) => matchesFilter(request, activeFilter))
      .sort((a, b) => {
        if (a.status === STATUS.COMPLETED && b.status !== STATUS.COMPLETED) return 1;
        if (b.status === STATUS.COMPLETED && a.status !== STATUS.COMPLETED) return -1;
        if (a.status === STATUS.REJECTED && b.status !== STATUS.REJECTED) return 1;
        if (b.status === STATUS.REJECTED && a.status !== STATUS.REJECTED) return -1;

        return new Date(b.createdOn) - new Date(a.createdOn);
      });
  }, [requests, activeFilter]);

  async function loadRequests() {
    setIsLoading(true);
    setError("");

    try {
      const response = await getMyArtistTattooRequests();
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setRequests(data || []);
      return data || [];
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function openRequest(request) {
    setSelectedRequest(request);
    setActiveAction("");
    setError("");
    setSuccess("");
    setResponseForm({ estimatedPrice: "", estimatedHours: "", responseMessage: "" });
    setSessions([createEmptySession()]);
    setExtraSessions([createEmptySession()]);
  }

  function closeRequest() {
    setSelectedRequest(null);
    setActiveAction("");
    setError("");
    setSuccess("");
  }

  function updateSessionList(setter, index, field, value) {
    setter((current) => {
      const copy = [...current];
      copy[index] = { ...copy[index], [field]: value };
      return copy;
    });
  }

  function buildSessionPayload(sessionList) {
    return {
      sessionsToBook: sessionList.length,
      additionalSessions: sessionList.length,
      priceForSession: sessionList.map((session) => Number(session.price)),
      durationHoursForSession: sessionList.map((session) => Number(session.durationHours)),
    };
  }

  async function runAction(action) {
    if (!selectedRequest) return;

    setError("");
    setSuccess("");

    try {
      let response;

      if (action === "create-response") {
        response = await createArtistResponse({
          tattooRequestId: selectedRequest.id,
          estimatedPrice: Number(responseForm.estimatedPrice),
          estimatedHours: Number(responseForm.estimatedHours),
          responseMessage: responseForm.responseMessage,
        });
      }

      if (action === "reject-request") {
        response = await rejectTattooRequest(selectedRequest.id);
      }

      if (action === "complete-consultation") {
        const payload = buildSessionPayload(sessions);
        delete payload.additionalSessions;
        response = await completeConsultation(selectedRequest.id, payload);
      }

      if (action === "reject-consultation") {
        response = await rejectConsultation(selectedRequest.id);
      }

      if (action === "add-sessions") {
        const payload = buildSessionPayload(extraSessions);
        delete payload.sessionsToBook;
        response = await addMoreSessions(selectedRequest.id, payload);
      }

      if (action === "complete-tattoo") {
        response = await completeTattoo(selectedRequest.id);
      }

      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Action completed successfully.");
      const updatedRequests = await loadRequests();
      const updatedSelected = updatedRequests?.find((item) => item.id === selectedRequest.id);
      if (updatedSelected) {
        setSelectedRequest(updatedSelected);
      }
      setActiveAction("");
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  function renderActionButtons(request) {
    if (request.status === STATUS.REJECTED || request.status === STATUS.COMPLETED) {
      return <p className="muted">No active actions for this request.</p>;
    }

    if (request.status === STATUS.SUBMITTED && !request.artistResponse) {
      return (
        <div className="action-row">
          <button className="primary-button" type="button" onClick={() => setActiveAction("response")}>
            Respond
          </button>
          <button className="danger-button" type="button" onClick={() => runAction("reject-request")}>
            Reject
          </button>
        </div>
      );
    }

    if (request.consultation && request.status === STATUS.WAITING_FOR_CONSULTATION) {
      return (
        <div className="action-row">
          <button className="primary-button" type="button" onClick={() => setActiveAction("complete-consultation")}>
            Complete consultation
          </button>
          <button className="danger-button" type="button" onClick={() => runAction("reject-consultation")}>
            Reject consultation
          </button>
        </div>
      );
    }

    if (request.status === STATUS.CONSULTATION_COMPLETED || request.status === STATUS.TATTOO_BOOKED || request.status === STATUS.IN_PROGRESS) {
      return (
        <div className="action-row">
          <button className="secondary-button" type="button" onClick={() => setActiveAction("add-sessions")}>
            Add more sessions
          </button>
          <button className="primary-button" type="button" onClick={() => runAction("complete-tattoo")}>
            Complete tattoo
          </button>
        </div>
      );
    }

    return <p className="muted">Waiting for the client to take the next step.</p>;
  }

  function renderRequestTiming(request) {
    const upcomingConsultation = getUpcomingConsultation(request);
    const upcomingTattooSession = getUpcomingTattooSession(request);

    return (
      <div className="info-list">
        {request.artistResponse && !request.consultation && request.status === STATUS.WAITING_FOR_CONSULTATION && (
          <p><span>Consultation:</span> Not booked yet</p>
        )}
        {request.consultation && (
          <p><span>Consultation:</span> {formatDateTime(request.consultation.startTime)}</p>
        )}
        {upcomingConsultation && (
          <p><span>Upcoming consultation:</span> {formatDateTime(upcomingConsultation.startTime)}</p>
        )}
        {upcomingTattooSession && (
          <p><span>Upcoming tattoo session:</span> {formatDateTime(upcomingTattooSession.startTime)}</p>
        )}
        {request.tattooSessions?.length > 0 && (
          <p><span>Sessions booked:</span> {request.tattooSessions.length}</p>
        )}
      </div>
    );
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header header-row">
          <div>
            <p className="subtitle">My Studio</p>
            <h1>Request workflow</h1>
            <p>Filter requests by workflow status and open each request to manage the right next action.</p>
          </div>
        </div>

        <div className="filter-tabs">
          {REQUEST_FILTERS.map((filter) => (
            <button
              key={filter.value}
              className={`filter-tab ${activeFilter === filter.value ? "filter-tab-active" : ""}`}
              type="button"
              onClick={() => setActiveFilter(filter.value)}
            >
              {filter.label}
              <span>{getFilterCount(requests, filter.value)}</span>
            </button>
          ))}
        </div>

        {isLoading && <p className="message">Loading requests...</p>}
        {error && !selectedRequest && <p className="error">{error}</p>}
        {!isLoading && !error && filteredRequests.length === 0 && (
          <p className="message">No requests in this category.</p>
        )}

        <div className="grid-2">
          {filteredRequests.map((request) => (
            <article className="card artist-card" key={request.id}>
              <div className="card-head">
                <div>
                  <p className="subtitle inline-subtitle">{getWorkflowStep(request)}</p>
                  <h2>{getRequestTitle(request)}</h2>
                </div>
                <span className={`status-pill ${getStatusClass(request.status)}`}>
                  {getStatusName(request.status)}
                </span>
              </div>

              <p className="muted clamp-text">{request.description}</p>

              <div className="info-list">
                {request.clientName && <p><span>Client:</span> {request.clientName}</p>}
                <p><span>Created:</span> {formatDate(request.createdOn)}</p>
              </div>
              {renderRequestTiming(request)}

              <button className="primary-button" type="button" onClick={() => openRequest(request)}>
                Open request
              </button>
            </article>
          ))}
        </div>
      </section>

      {selectedRequest && (
        <div className="modal-backdrop" onClick={closeRequest}>
          <section className="modal-card request-modal" onClick={(event) => event.stopPropagation()}>
            <div className="modal-head">
              <div>
                <p className="subtitle">{getWorkflowStep(selectedRequest)}</p>
                <h2>{getRequestTitle(selectedRequest)}</h2>
              </div>
              <button className="icon-button" type="button" onClick={closeRequest}>×</button>
            </div>

            <span className={`status-pill ${getStatusClass(selectedRequest.status)}`}>
              {getStatusName(selectedRequest.status)}
            </span>

            <div className="section workflow-section">
              <h3>Project progress</h3>
              <RequestWorkflowTimeline request={selectedRequest} />
            </div>

            <div className="section">
              <h3>Request details</h3>
              <p className="muted">{selectedRequest.description}</p>
              <div className="info-list">
                <p><span>Placement:</span> {selectedRequest.placement}</p>
                <p><span>Style:</span> {selectedRequest.tattooStyle || "Not provided"}</p>
                <p><span>Created:</span> {formatDate(selectedRequest.createdOn)}</p>
              </div>
              {renderRequestTiming(selectedRequest)}
            </div>

            <div className="section highlighted">
              <h3>Client contact</h3>
              <div className="info-list">
                <p><span>Name:</span> {selectedRequest.clientName || "Not provided"}</p>
                <p><span>Email:</span> {selectedRequest.clientEmail || "Not provided"}</p>
                <p><span>Phone:</span> {selectedRequest.clientPhoneNumber || "Not provided"}</p>
                <p><span>Location:</span> {[selectedRequest.clientCity, selectedRequest.clientCountry].filter(Boolean).join(", ") || "Not provided"}</p>
              </div>
            </div>

            {selectedRequest.images?.length > 0 && (
              <div className="section">
                <h3>Reference images</h3>
                <div className="image-grid">
                  {selectedRequest.images.map((image, index) => (
                    <img key={index} src={getImageUrl(image.imageUrl)} alt="Tattoo reference" />
                  ))}
                </div>
              </div>
            )}

            {selectedRequest.artistResponse && (
              <div className="section highlighted">
                <h3>Artist response</h3>
                <p className="muted">{selectedRequest.artistResponse.responseMessage}</p>
                <div className="small-list-row">
                  <span>{selectedRequest.artistResponse.estimatedPrice} BGN</span>
                  <span>{selectedRequest.artistResponse.estimatedHours} hours</span>
                  <span>{formatDate(selectedRequest.artistResponse.createdOn)}</span>
                </div>
              </div>
            )}

            {selectedRequest.consultation && (
              <div className="section">
                <h3>Consultation</h3>
                <div className="info-list">
                  <p><span>Start:</span> {formatDateTime(selectedRequest.consultation.startTime)}</p>
                  <p><span>End:</span> {formatDateTime(selectedRequest.consultation.endTime)}</p>
                  <p><span>Notes:</span> {selectedRequest.consultation.notes || "No notes"}</p>
                </div>
              </div>
            )}

            {selectedRequest.tattooSessions?.length > 0 && (
              <div className="section">
                <h3>Tattoo sessions</h3>
                <div className="small-list">
                  {selectedRequest.tattooSessions.map((session, index) => (
                    <div className="small-list-row" key={index}>
                      <span>Session {index + 1}</span>
                      <span>{formatDateTime(session.startTime)} - {formatTime(session.endTime)}</span>
                      <span>{session.priceForTheSession} BGN</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="section">
              <h3>Available actions</h3>
              {!activeAction && renderActionButtons(selectedRequest)}
            </div>

            {activeAction === "response" && (
              <div className="section action-panel">
                <h3>Create response</h3>
                <div className="form-row">
                  <div className="form-group">
                    <label>Estimated price</label>
                    <input type="number" step="0.01" value={responseForm.estimatedPrice} onChange={(event) => setResponseForm({ ...responseForm, estimatedPrice: event.target.value })} />
                  </div>
                  <div className="form-group">
                    <label>Estimated hours</label>
                    <input type="number" value={responseForm.estimatedHours} onChange={(event) => setResponseForm({ ...responseForm, estimatedHours: event.target.value })} />
                  </div>
                </div>
                <div className="form-group">
                  <label>Response message</label>
                  <textarea value={responseForm.responseMessage} onChange={(event) => setResponseForm({ ...responseForm, responseMessage: event.target.value })} />
                </div>
                <button className="primary-button" type="button" onClick={() => runAction("create-response")}>Send response</button>
              </div>
            )}

            {activeAction === "complete-consultation" && (
              <div className="section action-panel">
                <h3>Complete consultation</h3>
                <p className="muted">Set price and duration for each tattoo session the client should book.</p>
                {sessions.map((session, index) => (
                  <div className="form-row" key={index}>
                    <div className="form-group">
                      <label>Session {index + 1} price</label>
                      <input type="number" step="0.01" value={session.price} onChange={(event) => updateSessionList(setSessions, index, "price", event.target.value)} />
                    </div>
                    <div className="form-group">
                      <label>Duration hours</label>
                      <input type="number" value={session.durationHours} onChange={(event) => updateSessionList(setSessions, index, "durationHours", event.target.value)} />
                    </div>
                  </div>
                ))}
                <div className="action-row">
                  <button className="secondary-button" type="button" onClick={() => setSessions([...sessions, createEmptySession()])}>Add session</button>
                  <button className="primary-button" type="button" onClick={() => runAction("complete-consultation")}>Complete consultation</button>
                </div>
              </div>
            )}

            {activeAction === "add-sessions" && (
              <div className="section action-panel">
                <h3>Add more sessions</h3>
                {extraSessions.map((session, index) => (
                  <div className="form-row" key={index}>
                    <div className="form-group">
                      <label>Extra session {index + 1} price</label>
                      <input type="number" step="0.01" value={session.price} onChange={(event) => updateSessionList(setExtraSessions, index, "price", event.target.value)} />
                    </div>
                    <div className="form-group">
                      <label>Duration hours</label>
                      <input type="number" value={session.durationHours} onChange={(event) => updateSessionList(setExtraSessions, index, "durationHours", event.target.value)} />
                    </div>
                  </div>
                ))}
                <div className="action-row">
                  <button className="secondary-button" type="button" onClick={() => setExtraSessions([...extraSessions, createEmptySession()])}>Add another</button>
                  <button className="primary-button" type="button" onClick={() => runAction("add-sessions")}>Save sessions</button>
                </div>
              </div>
            )}

            {error && <p className="error">{error}</p>}
            {success && <p className="success">{success}</p>}
          </section>
        </div>
      )}
    </main>
  );
}

export default ArtistRequestsPage;
