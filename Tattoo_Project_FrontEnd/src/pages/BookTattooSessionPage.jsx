import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { getBookingAvailability } from "../api/tattooRequestApi";
import { createTattooSession } from "../api/tattooSessionApi";
import { readResponse } from "../api/http";
import { formatDateTime, formatTime } from "../utils/format";

function toApiDateTime(value) {
  // Backend schedule validation treats DateTime as the artist's local schedule time.
  // Do not convert the slot to UTC with toISOString(), because 09:00 would become
  // 06:00/07:00 depending on timezone and then fail the schedule check.
  return value;
}

function BookingSlotPage() {
  const params = useParams();
  const navigate = useNavigate();
  const requestId = params.tattooRequestId;
  const bookingType = "session";
  const title = "Book tattoo session";
  const subtitle = "Tattoo Session";
  const actionLabel = "Book tattoo session";
  const [availability, setAvailability] = useState(null);
  const [selectedDate, setSelectedDate] = useState("");
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!requestId) {
      setError("Open this page from a booking card. The request id is handled automatically.");
      setIsLoading(false);
      return;
    }
    loadAvailability();
  }, [requestId]);

  async function loadAvailability() {
    setIsLoading(true);
    setError("");
    try {
      const response = await getBookingAvailability(requestId, bookingType);
      const data = await readResponse(response);
      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }
      setAvailability(data);
      const firstDay = data.days?.find((day) => day.slots?.length > 0);
      if (firstDay) setSelectedDate(firstDay.date);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  const selectedDay = useMemo(() => {
    return availability?.days?.find((day) => day.date === selectedDate);
  }, [availability, selectedDate]);

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (!selectedSlot) {
      setError("Choose an available time slot first.");
      return;
    }

    try {
      const payload = { tattooRequestId: Number(requestId), startTime: toApiDateTime(selectedSlot.startTime) };
      const response = await createTattooSession(payload);
      const data = await readResponse(response);
      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }
      setSuccess("Tattoo session booked successfully.");
      setTimeout(() => navigate("/bookings"), 900);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  if (!requestId) {
    return (
      <main className="center-container">
        <section className="card form-card">
          <div className="header">
            <p className="subtitle">{subtitle}</p>
            <h1>{title}</h1>
            <p className="error">Open this page from Bookings. You should never type a request id manually.</p>
          </div>
          <Link className="primary-button" to="/bookings">Back to Bookings</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">{subtitle}</p>
          <h1>{title}</h1>
          <p>Choose an available slot. Unavailable days are grey and only valid rounded time slots are clickable.</p>
        </div>

        {isLoading && <p className="message">Loading available slots...</p>}
        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}

        {availability && (
          <form className="calendar-layout" onSubmit={handleSubmit}>
            <section className="card calendar-card">
              <h2>Available days</h2>
              <p className="muted">Duration: {availability.durationMinutes} minutes</p>
              <div className="booking-day-grid">
                {availability.days?.map((day) => (
                  <button
                    className={`booking-day ${day.slots?.length ? "booking-day-available" : "booking-day-disabled"} ${selectedDate === day.date ? "booking-day-selected" : ""}`}
                    type="button"
                    key={day.date}
                    disabled={!day.slots?.length}
                    onClick={() => {
                      setSelectedDate(day.date);
                      setSelectedSlot(null);
                    }}
                  >
                    <strong>{new Date(`${day.date}T00:00:00`).toLocaleDateString(undefined, { weekday: "short", day: "numeric", month: "short" })}</strong>
                    <span>{day.slots?.length ? `${day.slots.length} free slots` : day.reason || "Busy"}</span>
                  </button>
                ))}
              </div>
            </section>

            <aside className="card form-card calendar-side-panel">
              <h2>{selectedDate ? new Date(`${selectedDate}T00:00:00`).toLocaleDateString() : "Choose a day"}</h2>
              <div className="section">
                <h3>Available times</h3>
                {!selectedDay?.slots?.length && <p className="muted">The artist is busy or outside schedule for this day.</p>}
                <div className="booking-slot-grid">
                  {selectedDay?.slots?.map((slot) => (
                    <button
                      key={slot.startTime}
                      className={`booking-slot ${selectedSlot?.startTime === slot.startTime ? "booking-slot-selected" : ""}`}
                      type="button"
                      onClick={() => setSelectedSlot(slot)}
                    >
                      <strong>{slot.label}</strong>
                      <span>{formatTime(slot.endTime)}</span>
                    </button>
                  ))}
                </div>
              </div>

              {selectedSlot && (
                <div className="section highlighted">
                  <h3>Selected slot</h3>
                  <p>{formatDateTime(selectedSlot.startTime)} - {formatTime(selectedSlot.endTime)}</p>
                </div>
              )}

              

              <button className="primary-button full-button" disabled={!selectedSlot}>{actionLabel}</button>
            </aside>
          </form>
        )}
      </section>
    </main>
  );
}

export default BookingSlotPage;
