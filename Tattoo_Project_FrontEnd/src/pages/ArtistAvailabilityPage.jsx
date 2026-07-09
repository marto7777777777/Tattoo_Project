import { useEffect, useMemo, useState } from "react";
import {
  createUnavailableDate,
  deleteUnavailableDate,
  getMyUnavailableDates,
} from "../api/artistUnavailableDateApi";
import { readResponse } from "../api/http";
import { formatDateTime, toApiDateTime } from "../utils/format";

function getTodayDateInputValue() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function toDateTimeLocalValue(date, time) {
  if (!date || !time) {
    return "";
  }

  return `${date}T${time}`;
}

function ArtistAvailabilityPage() {
  const [unavailableDates, setUnavailableDates] = useState([]);
  const [form, setForm] = useState({
    mode: "fullDay",
    startDate: getTodayDateInputValue(),
    endDate: getTodayDateInputValue(),
    startDateTime: "",
    endDateTime: "",
  });
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const sortedUnavailableDates = useMemo(() => {
    return [...unavailableDates].sort(
      (a, b) => new Date(a.startDateTime) - new Date(b.startDateTime)
    );
  }, [unavailableDates]);

  useEffect(() => {
    loadUnavailableDates();
  }, []);

  function handleChange(event) {
    const { name, value } = event.target;

    setForm((currentForm) => ({
      ...currentForm,
      [name]: value,
    }));
  }

  async function loadUnavailableDates() {
    setError("");
    setIsLoading(true);

    try {
      const response = await getMyUnavailableDates();
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setUnavailableDates(Array.isArray(data) ? data : []);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  function getSelectedPeriod() {
    if (form.mode === "fullDay") {
      const startDateTime = toDateTimeLocalValue(form.startDate, "00:00");

      const endDate = new Date(`${form.endDate}T00:00`);
      endDate.setDate(endDate.getDate() + 1);

      const year = endDate.getFullYear();
      const month = String(endDate.getMonth() + 1).padStart(2, "0");
      const day = String(endDate.getDate()).padStart(2, "0");
      const endDateTime = `${year}-${month}-${day}T00:00`;

      return { startDateTime, endDateTime };
    }

    return {
      startDateTime: form.startDateTime,
      endDateTime: form.endDateTime,
    };
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setSuccess("");

    const selectedPeriod = getSelectedPeriod();

    if (!selectedPeriod.startDateTime || !selectedPeriod.endDateTime) {
      setError("Please select a valid unavailable period.");
      return;
    }

    if (new Date(selectedPeriod.startDateTime) >= new Date(selectedPeriod.endDateTime)) {
      setError("Start time must be before end time.");
      return;
    }

    const unavailableDateData = {
      startDateTime: toApiDateTime(selectedPeriod.startDateTime),
      endDateTime: toApiDateTime(selectedPeriod.endDateTime),
    };

    setIsSubmitting(true);

    try {
      const response = await createUnavailableDate(unavailableDateData);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Unavailable period created successfully.");
      await loadUnavailableDates();
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleDelete(id) {
    setError("");
    setSuccess("");

    const shouldDelete = window.confirm(
      "Are you sure you want to remove this unavailable period?"
    );

    if (!shouldDelete) {
      return;
    }

    try {
      const response = await deleteUnavailableDate(id);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Unavailable period removed successfully.");
      await loadUnavailableDates();
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="page-shell">
      <section className="container availability-layout">
        <div className="header">
          <p className="subtitle">Artist Availability</p>
          <h1>Manage days off</h1>
          <p>
            Block full days or custom time periods when you are not available for
            consultations or tattoo sessions.
          </p>
        </div>

        <div className="grid-2 availability-grid">
          <section className="card form-card">
            <h2>Create unavailable period</h2>
            <p className="muted">
              If you already have a consultation or tattoo session in this
              period, the backend will reject the request until you cancel it.
            </p>

            <form className="form" onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Period type</label>
                <select name="mode" value={form.mode} onChange={handleChange}>
                  <option value="fullDay">Full day or date range</option>
                  <option value="customTime">Custom hours</option>
                </select>
              </div>

              {form.mode === "fullDay" && (
                <div className="form-row">
                  <div className="form-group">
                    <label>From date</label>
                    <input
                      name="startDate"
                      type="date"
                      value={form.startDate}
                      onChange={handleChange}
                    />
                  </div>

                  <div className="form-group">
                    <label>To date</label>
                    <input
                      name="endDate"
                      type="date"
                      value={form.endDate}
                      onChange={handleChange}
                    />
                  </div>
                </div>
              )}

              {form.mode === "customTime" && (
                <>
                  <div className="form-group">
                    <label>Start date and time</label>
                    <input
                      name="startDateTime"
                      type="datetime-local"
                      value={form.startDateTime}
                      onChange={handleChange}
                    />
                  </div>

                  <div className="form-group">
                    <label>End date and time</label>
                    <input
                      name="endDateTime"
                      type="datetime-local"
                      value={form.endDateTime}
                      onChange={handleChange}
                    />
                  </div>
                </>
              )}

              {error && <p className="error">{error}</p>}
              {success && <p className="success">{success}</p>}

              <button className="primary-button" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Create unavailable period"}
              </button>
            </form>
          </section>

          <section className="card form-card">
            <div className="availability-list-header">
              <div>
                <h2>My unavailable periods</h2>
                <p className="muted">Upcoming days off and blocked hours.</p>
              </div>

              <button
                type="button"
                className="secondary-button"
                onClick={loadUnavailableDates}
                disabled={isLoading}
              >
                {isLoading ? "Loading..." : "Refresh"}
              </button>
            </div>

            {isLoading && <p className="message">Loading unavailable periods...</p>}

            {!isLoading && sortedUnavailableDates.length === 0 && (
              <p className="message">You do not have unavailable periods yet.</p>
            )}

            {!isLoading && sortedUnavailableDates.length > 0 && (
              <div className="unavailable-list">
                {sortedUnavailableDates.map((item) => (
                  <article className="unavailable-item" key={item.id}>
                    <div>
                      <p className="unavailable-dates">
                        {formatDateTime(item.startDateTime)}
                      </p>
                      <p className="muted">to {formatDateTime(item.endDateTime)}</p>
                    </div>

                    <button
                      type="button"
                      className="danger-button"
                      onClick={() => handleDelete(item.id)}
                    >
                      Delete
                    </button>
                  </article>
                ))}
              </div>
            )}
          </section>
        </div>
      </section>
    </main>
  );
}

export default ArtistAvailabilityPage;
