import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { completeConsultation } from "../api/consultationApi";
import { readResponse } from "../api/http";

function CompleteConsultationPage() {
  const { tattooRequestId } = useParams();
  const navigate = useNavigate();
  const requestId = Number(tattooRequestId);
  const hasValidRequest = Number.isInteger(requestId) && requestId > 0;
  const [sessions, setSessions] = useState([{ price: "", durationHours: "" }]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function updateSession(index, field, value) {
    const copy = [...sessions];
    copy[index] = { ...copy[index], [field]: value };
    setSessions(copy);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (!hasValidRequest) {
      setError("This tattoo request could not be found. Open it again from My requests.");
      return;
    }

    const data = {
      sessionsToBook: sessions.length,
      priceForSession: sessions.map((session) => Number(session.price)),
      durationHoursForSession: sessions.map((session) => Number(session.durationHours)),
    };

    try {
      const response = await completeConsultation(requestId, data);
      const result = await readResponse(response);
      if (!response.ok) {
        setError(typeof result === "string" ? result : JSON.stringify(result));
        return;
      }

      setSuccess("Consultation completed successfully.");
      setTimeout(() => navigate("/my-studio"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="center-container artist-action-page">
      <section className="card form-card artist-action-card">
        <div className="header">
          <p className="subtitle">Complete Consultation</p>
          <h1>Define planned sessions</h1>
          <p>Set price and duration for every tattoo session the client can book.</p>
        </div>

        {!hasValidRequest ? (
          <div className="form">
            <p className="error">This tattoo request could not be found. Open it again from My requests.</p>
            <button className="secondary-button" type="button" onClick={() => navigate("/my-studio/requests")}>
              Back to requests
            </button>
          </div>
        ) : (
          <form className="form" onSubmit={handleSubmit}>
            <div className="section artist-session-fields">
              <h2>Sessions</h2>
              {sessions.map((session, index) => (
                <div className="form-row" key={index}>
                  <div className="form-group">
                    <label>{`Session ${index + 1} price`}</label>
                    <input type="number" step="0.01" value={session.price} onChange={(event) => updateSession(index, "price", event.target.value)} />
                  </div>
                  <div className="form-group">
                    <label>Duration hours</label>
                    <input type="number" value={session.durationHours} onChange={(event) => updateSession(index, "durationHours", event.target.value)} />
                  </div>
                </div>
              ))}
              <button type="button" className="secondary-button" onClick={() => setSessions([...sessions, { price: "", durationHours: "" }])}>
                Add session
              </button>
            </div>

            {error && <p className="error">{error}</p>}
            {success && <p className="success">{success}</p>}
            <div className="action-row">
              <button className="primary-button">Complete Consultation</button>
            </div>
          </form>
        )}
      </section>
    </main>
  );
}

export default CompleteConsultationPage;
