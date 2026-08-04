export const ARTIST_RESPONSE_PATH = {
  CONSULTATION_FIRST: 0,
  DIRECT_TO_SESSIONS: 1,
};

export function createArtistResponseForm() {
  return {
    workflowPath: null,
    estimatedPrice: "",
    estimatedHours: "",
    responseMessage: "",
    sessions: [{ price: "", durationHours: "" }],
  };
}

export function buildArtistResponsePayload(form, tattooRequestId) {
  const directToSessions = form.workflowPath === ARTIST_RESPONSE_PATH.DIRECT_TO_SESSIONS;
  const prices = form.sessions.map((session) => Number(session.price));
  const durations = form.sessions.map((session) => Number(session.durationHours));

  return {
    tattooRequestId: Number(tattooRequestId),
    workflowPath: form.workflowPath,
    estimatedPrice: directToSessions
      ? prices.reduce((total, price) => total + price, 0)
      : Number(form.estimatedPrice),
    estimatedHours: directToSessions
      ? durations.reduce((total, duration) => total + duration, 0)
      : Number(form.estimatedHours),
    responseMessage: form.responseMessage,
    sessionsToBook: directToSessions ? form.sessions.length : null,
    priceForSession: directToSessions ? prices : null,
    durationHoursForSession: directToSessions ? durations : null,
  };
}

function ArtistResponseWorkflowFields({ form, setForm }) {
  const selectPath = (workflowPath) => setForm((current) => ({ ...current, workflowPath }));

  const updateSession = (index, field, value) => {
    setForm((current) => ({
      ...current,
      sessions: current.sessions.map((session, sessionIndex) =>
        sessionIndex === index ? { ...session, [field]: value } : session),
    }));
  };

  const addSession = () => setForm((current) => ({
    ...current,
    sessions: [...current.sessions, { price: "", durationHours: "" }],
  }));

  const removeSession = () => setForm((current) => ({
    ...current,
    sessions: current.sessions.slice(0, -1),
  }));

  return (
    <>
      <section className="artist-response-workflow-choice">
        <p className="subtitle inline-subtitle">Project workflow</p>
        <h3>How should this project continue?</h3>
        <p className="muted">Choose whether the client should book a consultation or go directly to tattoo sessions.</p>
        <div className="studio-choice-grid artist-response-choice-grid">
          <button
            type="button"
            aria-pressed={form.workflowPath === ARTIST_RESPONSE_PATH.CONSULTATION_FIRST}
            className={`studio-choice-card ${form.workflowPath === ARTIST_RESPONSE_PATH.CONSULTATION_FIRST ? "selected" : ""}`}
            onClick={() => selectPath(ARTIST_RESPONSE_PATH.CONSULTATION_FIRST)}
          >
            <strong>Consultation first</strong>
            <span>The client books a consultation before the final tattoo sessions are planned.</span>
          </button>
          <button
            type="button"
            aria-pressed={form.workflowPath === ARTIST_RESPONSE_PATH.DIRECT_TO_SESSIONS}
            className={`studio-choice-card ${form.workflowPath === ARTIST_RESPONSE_PATH.DIRECT_TO_SESSIONS ? "selected" : ""}`}
            onClick={() => selectPath(ARTIST_RESPONSE_PATH.DIRECT_TO_SESSIONS)}
          >
            <strong>Go directly to sessions</strong>
            <span>Skip the consultation and let the client book the planned tattoo sessions immediately.</span>
          </button>
        </div>
      </section>

      {form.workflowPath === ARTIST_RESPONSE_PATH.CONSULTATION_FIRST && (
        <section className="artist-response-path-details">
          <p className="artist-estimate-notice">Price and duration are preliminary estimates. Final values are confirmed after the consultation.</p>
          <div className="form-row">
            <div className="form-group">
              <label>Indicative price estimate</label>
              <input type="number" min="0.01" step="0.01" value={form.estimatedPrice} onChange={(event) => setForm((current) => ({ ...current, estimatedPrice: event.target.value }))} required />
            </div>
            <div className="form-group">
              <label>Indicative duration estimate (hours)</label>
              <input type="number" min="1" value={form.estimatedHours} onChange={(event) => setForm((current) => ({ ...current, estimatedHours: event.target.value }))} required />
            </div>
          </div>
        </section>
      )}

      {form.workflowPath === ARTIST_RESPONSE_PATH.DIRECT_TO_SESSIONS && (
        <section className="artist-response-path-details direct-session-plan">
          <p className="subtitle inline-subtitle">Consultation skipped</p>
          <h3>Plan the tattoo sessions</h3>
          <p className="muted">Set the exact price and duration of every session the client will be able to book.</p>
          {form.sessions.map((session, index) => (
            <div className="direct-session-row" key={index}>
              <strong>Session {index + 1}</strong>
              <div className="form-row">
                <div className="form-group">
                  <label>{`Session ${index + 1} price`}</label>
                  <input type="number" min="0.01" step="0.01" value={session.price} onChange={(event) => updateSession(index, "price", event.target.value)} required />
                </div>
                <div className="form-group">
                  <label>Duration hours</label>
                  <input type="number" min="1" value={session.durationHours} onChange={(event) => updateSession(index, "durationHours", event.target.value)} required />
                </div>
              </div>
            </div>
          ))}
          <div className="action-row">
            <button className="secondary-button" type="button" onClick={addSession}>Add session</button>
            {form.sessions.length > 1 && <button className="danger-button" type="button" onClick={removeSession}>Remove last session</button>}
          </div>
        </section>
      )}
    </>
  );
}

export default ArtistResponseWorkflowFields;
