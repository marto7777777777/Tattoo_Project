import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getMyProfile } from "../api/profileApi";
import { updateArtistProfile } from "../api/artistApi";
import {
  createUnavailableDate,
  deleteUnavailableDate,
  getMyUnavailableDates,
} from "../api/artistUnavailableDateApi";
import { getMyArtistTattooRequests } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";
import { formatDateTime, getStatusClass, getStatusName } from "../utils/format";

const EVENT_FILTERS = [
  { label: "All", value: "all" },
  { label: "Consultations", value: "consultation" },
  { label: "Tattoo sessions", value: "tattoo-session" },
  { label: "Days off", value: "unavailable" },
];

const emptySchedule = { dayOfWeek: "", startTime: "", endTime: "", scheduleType: "" };

function startOfMonth(date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addMonths(date, value) {
  return new Date(date.getFullYear(), date.getMonth() + value, 1);
}

function toDateKey(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function formatMonth(date) {
  return date.toLocaleDateString(undefined, { month: "long", year: "numeric" });
}

function getCalendarDays(monthDate) {
  const firstDay = startOfMonth(monthDate);
  const startOffset = firstDay.getDay() === 0 ? 6 : firstDay.getDay() - 1;
  const calendarStart = new Date(firstDay);
  calendarStart.setDate(firstDay.getDate() - startOffset);

  return Array.from({ length: 42 }, (_, index) => {
    const day = new Date(calendarStart);
    day.setDate(calendarStart.getDate() + index);
    return day;
  });
}

function getEventDateKey(value) {
  return toDateKey(new Date(value));
}

function getEventTimeRange(start, end) {
  const startDate = new Date(start);
  const endDate = new Date(end);

  return `${startDate.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  })} - ${endDate.toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  })}`;
}

function eventMatchesFilter(event, filter) {
  return filter === "all" || event.type === filter;
}

function ArtistSchedulePage() {
  const [monthDate, setMonthDate] = useState(startOfMonth(new Date()));
  const [selectedDateKey, setSelectedDateKey] = useState(toDateKey(new Date()));
  const [eventFilter, setEventFilter] = useState("all");
  const [requests, setRequests] = useState([]);
  const [unavailableDates, setUnavailableDates] = useState([]);
  const [mode, setMode] = useState("full-day");
  const [form, setForm] = useState({
    date: toDateKey(new Date()),
    startTime: "09:00",
    endTime: "18:00",
    endDate: toDateKey(new Date()),
  });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [profile, setProfile] = useState(null);
  const [showScheduleEditor, setShowScheduleEditor] = useState(false);
  const [scheduleForm, setScheduleForm] = useState([emptySchedule]);

  useEffect(() => {
    loadCalendarData();
    loadProfile();
  }, []);

  async function loadProfile() {
    try {
      const data = await getMyProfile();
      setProfile(data);
      const schedules = data.artist?.schedules?.length
        ? data.artist.schedules.map((schedule) => ({
            dayOfWeek: String(schedule.dayOfWeek),
            startTime: String(schedule.startTime).slice(0, 5),
            endTime: String(schedule.endTime).slice(0, 5),
            scheduleType: String(schedule.scheduleType),
          }))
        : [emptySchedule];
      setScheduleForm(schedules);
    } catch {
      // Schedule editing is optional; calendar can still load.
    }
  }

  const allEventsByDate = useMemo(() => {
    const result = {};

    function addEvent(dateKey, event) {
      if (!result[dateKey]) result[dateKey] = [];
      result[dateKey].push(event);
    }

    requests.forEach((request) => {
      if (request.consultation) {
        addEvent(getEventDateKey(request.consultation.startTime), {
          type: "consultation",
          label: "Consultation",
          title: request.placement || "Consultation",
          startTime: request.consultation.startTime,
          endTime: request.consultation.endTime,
          requestId: request.id,
          request,
        });
      }

      request.tattooSessions?.forEach((session, index) => {
        addEvent(getEventDateKey(session.startTime), {
          type: "tattoo-session",
          label: "Tattoo session",
          title: `${request.placement || "Tattoo session"} · Session ${index + 1}`,
          startTime: session.startTime,
          endTime: session.endTime,
          requestId: request.id,
          request,
        });
      });
    });

    unavailableDates.forEach((period) => {
      const start = new Date(period.startDateTime);
      const end = new Date(period.endDateTime);
      const cursor = new Date(start.getFullYear(), start.getMonth(), start.getDate());
      const finalDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());

      while (cursor <= finalDay) {
        addEvent(toDateKey(cursor), {
          type: "unavailable",
          label: "Day off",
          title: "Unavailable",
          startTime: period.startDateTime,
          endTime: period.endDateTime,
          unavailableId: period.id,
        });
        cursor.setDate(cursor.getDate() + 1);
      }
    });

    Object.values(result).forEach((events) => {
      events.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
    });

    return result;
  }, [requests, unavailableDates]);

  const eventsByDate = useMemo(() => {
    const result = {};

    Object.entries(allEventsByDate).forEach(([dateKey, events]) => {
      result[dateKey] = events.filter((event) => eventMatchesFilter(event, eventFilter));
    });

    return result;
  }, [allEventsByDate, eventFilter]);

  const scheduledWeekdays = useMemo(() => new Set((profile?.artist?.schedules || []).map((schedule) => Number(schedule.dayOfWeek))), [profile]);
  const selectedDayDate = new Date(`${selectedDateKey}T00:00:00`);
  const selectedDayOutsideSchedule = profile?.artist && !scheduledWeekdays.has(selectedDayDate.getDay());
  const selectedEvents = eventsByDate[selectedDateKey] || [];

  async function loadCalendarData() {
    setError("");
    setIsLoading(true);

    try {
      const [requestsResponse, unavailableResponse] = await Promise.all([
        getMyArtistTattooRequests(),
        getMyUnavailableDates(),
      ]);

      const requestsData = await readResponse(requestsResponse);
      const unavailableData = await readResponse(unavailableResponse);

      if (!requestsResponse.ok) {
        setError(typeof requestsData === "string" ? requestsData : JSON.stringify(requestsData));
        return;
      }

      if (!unavailableResponse.ok) {
        setError(typeof unavailableData === "string" ? unavailableData : JSON.stringify(unavailableData));
        return;
      }

      setRequests(requestsData || []);
      setUnavailableDates(unavailableData || []);
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleCreateUnavailable(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    let startDateTime;
    let endDateTime;

    if (mode === "full-day") {
      startDateTime = `${form.date}T00:00:00`;
      const nextDay = new Date(`${form.date}T00:00:00`);
      nextDay.setDate(nextDay.getDate() + 1);
      endDateTime = `${toDateKey(nextDay)}T00:00:00`;
    } else if (mode === "date-range") {
      startDateTime = `${form.date}T00:00:00`;
      const end = new Date(`${form.endDate}T00:00:00`);
      end.setDate(end.getDate() + 1);
      endDateTime = `${toDateKey(end)}T00:00:00`;
    } else {
      startDateTime = `${form.date}T${form.startTime}:00`;
      endDateTime = `${form.date}T${form.endTime}:00`;
    }

    try {
      const response = await createUnavailableDate({ startDateTime, endDateTime });
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Unavailable period created successfully.");
      setSelectedDateKey(form.date);
      await loadCalendarData();
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  function updateSchedule(index, field, value) {
    setScheduleForm((current) => {
      const copy = [...current];
      copy[index] = { ...copy[index], [field]: value };
      return copy;
    });
  }

  function addScheduleRow() {
    setScheduleForm((current) => [...current, emptySchedule]);
  }

  function removeScheduleRow(index) {
    setScheduleForm((current) => current.filter((_, itemIndex) => itemIndex !== index));
  }

  async function handleUpdateSchedule(event) {
    event.preventDefault();
    if (!profile?.artist) {
      setError("Artist profile was not loaded yet.");
      return;
    }

    setError("");
    setSuccess("");

    const schedules = scheduleForm
      .filter((schedule) => schedule.dayOfWeek !== "" && schedule.startTime && schedule.endTime && schedule.scheduleType !== "")
      .map((schedule) => ({
        dayOfWeek: Number(schedule.dayOfWeek),
        startTime: `${schedule.startTime}:00`,
        endTime: `${schedule.endTime}:00`,
        scheduleType: Number(schedule.scheduleType),
      }));

    if (!schedules.some((schedule) => schedule.scheduleType === 1) || !schedules.some((schedule) => schedule.scheduleType === 0)) {
      setError("Add at least one consultation schedule and one tattoo session schedule.");
      return;
    }

    const payload = {
      firstName: profile.firstName,
      lastName: profile.lastName,
      email: profile.email,
      studioName: profile.artist.studioName,
      description: profile.artist.description,
      consultationDurationMinutes: profile.artist.consultationDurationMinutes,
      studioAddress: profile.artist.studioAddress,
      studioCity: profile.artist.studioCity,
      studioCountry: profile.artist.studioCountry,
      studioLatitude: null,
      studioLongitude: null,
      phoneNumber: profile.phoneNumber || "",
      offersOnlineConsultation: profile.artist.offersOnlineConsultation,
      requiresDeposit: profile.artist.requiresDeposit,
      depositAmount: profile.artist.depositAmount,
      requirements: (profile.artist.requirements || []).map((requirement) => ({ description: requirement.description })),
      portfolioImages: (profile.artist.portfolioImages || []).map((image) => ({ imageUrl: image.imageUrl })),
      schedules,
    };

    try {
      const response = await updateArtistProfile(payload);
      const data = await readResponse(response);
      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }
      setSuccess("Schedule updated successfully.");
      setShowScheduleEditor(false);
      await loadProfile();
      await loadCalendarData();
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  async function handleDeleteUnavailable(id) {
    setError("");
    setSuccess("");

    try {
      const response = await deleteUnavailableDate(id);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Unavailable period deleted successfully.");
      await loadCalendarData();
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="page-shell">
      <section className="container">
        <div className="header header-row">
          <div>
            <p className="subtitle">My Studio</p>
            <h1>Calendar</h1>
            <p>View consultations, tattoo sessions, and days off on one calendar.</p>
          </div>
          <div className="action-row">
            <button className="secondary-button" type="button" onClick={() => setShowScheduleEditor((current) => !current)}>
              {showScheduleEditor ? "Close schedule editor" : "Edit Schedule"}
            </button>
            <Link className="secondary-button" to="/my-studio/requests">Open Requests</Link>
          </div>
        </div>

        {showScheduleEditor && (
          <form className="card form-card form-card-large section" onSubmit={handleUpdateSchedule}>
            <h2>Edit working schedule</h2>
            <p className="muted">Update the same consultation and tattoo session schedule you added during artist profile creation.</p>
            {scheduleForm.map((schedule, index) => (
              <div className="schedule-row" key={index}>
                <div className="form-group">
                  <label>Day</label>
                  <select value={schedule.dayOfWeek} onChange={(event) => updateSchedule(index, "dayOfWeek", event.target.value)}>
                    <option value="">Choose</option>
                    <option value="1">Monday</option>
                    <option value="2">Tuesday</option>
                    <option value="3">Wednesday</option>
                    <option value="4">Thursday</option>
                    <option value="5">Friday</option>
                    <option value="6">Saturday</option>
                    <option value="0">Sunday</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>Start</label>
                  <input type="time" value={schedule.startTime} onChange={(event) => updateSchedule(index, "startTime", event.target.value)} />
                </div>
                <div className="form-group">
                  <label>End</label>
                  <input type="time" value={schedule.endTime} onChange={(event) => updateSchedule(index, "endTime", event.target.value)} />
                </div>
                <div className="form-group">
                  <label>Type</label>
                  <select value={schedule.scheduleType} onChange={(event) => updateSchedule(index, "scheduleType", event.target.value)}>
                    <option value="">Choose</option>
                    <option value="1">Consultation</option>
                    <option value="0">Tattoo session</option>
                  </select>
                </div>
                <button className="danger-button compact-button" type="button" onClick={() => removeScheduleRow(index)}>Remove</button>
              </div>
            ))}
            <div className="action-row">
              <button className="secondary-button" type="button" onClick={addScheduleRow}>Add row</button>
              <button className="primary-button">Save schedule</button>
            </div>
          </form>
        )}

        <div className="filter-tabs">
          {EVENT_FILTERS.map((filter) => (
            <button
              key={filter.value}
              className={`filter-tab ${eventFilter === filter.value ? "filter-tab-active" : ""}`}
              type="button"
              onClick={() => setEventFilter(filter.value)}
            >
              {filter.label}
            </button>
          ))}
        </div>

        {isLoading && <p className="message">Loading calendar...</p>}
        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}

        <div className="calendar-layout">
          <section className="card calendar-card">
            <div className="calendar-toolbar">
              <button className="secondary-button" type="button" onClick={() => setMonthDate(addMonths(monthDate, -1))}>
                Previous
              </button>
              <h2>{formatMonth(monthDate)}</h2>
              <button className="secondary-button" type="button" onClick={() => setMonthDate(addMonths(monthDate, 1))}>
                Next
              </button>
            </div>

            <div className="calendar-legend">
              <span><i className="event-dot event-consultation" /> Consultation</span>
              <span><i className="event-dot event-tattoo-session" /> Tattoo session</span>
              <span><i className="event-dot event-unavailable" /> Day off</span>
            </div>

            <div className="calendar-weekdays">
              <span>Mon</span>
              <span>Tue</span>
              <span>Wed</span>
              <span>Thu</span>
              <span>Fri</span>
              <span>Sat</span>
              <span>Sun</span>
            </div>

            <div className="calendar-grid">
              {getCalendarDays(monthDate).map((day) => {
                const key = toDateKey(day);
                const events = eventsByDate[key] || [];
                const isCurrentMonth = day.getMonth() === monthDate.getMonth();
                const isSelected = key === selectedDateKey;
                const hasDayOff = events.some((event) => event.type === "unavailable");
                const isOutsideSchedule = Boolean(profile?.artist) && !scheduledWeekdays.has(day.getDay());

                return (
                  <button
                    className={`calendar-day ${isCurrentMonth ? "" : "calendar-day-muted"} ${isSelected ? "calendar-day-selected" : ""} ${hasDayOff || isOutsideSchedule ? "calendar-day-off" : ""}`}
                    key={key}
                    type="button"
                    onClick={() => {
                      setSelectedDateKey(key);
                      setForm((current) => ({ ...current, date: key, endDate: key }));
                    }}
                  >
                    <span className="calendar-day-number">{day.getDate()}</span>
                    <div className="calendar-event-dots">
                      {events.slice(0, 4).map((event, index) => (
                        <span className={`event-dot event-${event.type}`} key={`${event.type}-${index}`} />
                      ))}
                      {events.length > 4 && <span className="event-more">+{events.length - 4}</span>}
                    </div>
                  </button>
                );
              })}
            </div>
          </section>

          <aside className="card form-card calendar-side-panel">
            <h2>{new Date(`${selectedDateKey}T00:00:00`).toLocaleDateString()}</h2>

            <div className="section">
              <h3>Day events</h3>
              {selectedDayOutsideSchedule && <p className="calendar-closed-message">Closed — this weekday is not included in the artist schedule.</p>}
              {selectedEvents.length === 0 && <p className="muted">No events for this filter.</p>}

              <div className="small-list">
                {selectedEvents.map((event, index) => (
                  <div className={`calendar-event-item event-border-${event.type}`} key={`${event.type}-${index}-${event.startTime}`}>
                    <div>
                      <p className="subtitle inline-subtitle">{event.label}</p>
                      <strong>{event.title}</strong>
                      <p className="muted">{getEventTimeRange(event.startTime, event.endTime)}</p>
                      {event.request && (
                        <span className={`status-pill ${getStatusClass(event.request.status)}`}>
                          {getStatusName(event.request.status)}
                        </span>
                      )}
                    </div>

                    {event.requestId && (
                      <Link className="secondary-button compact-button" to={`/my-studio/requests?requestId=${event.requestId}`}>
                        Open request
                      </Link>
                    )}

                    {event.unavailableId && (
                      <button className="danger-button compact-button" type="button" onClick={() => handleDeleteUnavailable(event.unavailableId)}>
                        Delete
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <form className="section" onSubmit={handleCreateUnavailable}>
              <h3>Add day off or break</h3>

              <div className="form-group">
                <label>Type</label>
                <select value={mode} onChange={(event) => setMode(event.target.value)}>
                  <option value="full-day">Full day</option>
                  <option value="date-range">Date range</option>
                  <option value="custom-hours">Custom hours</option>
                </select>
              </div>

              <div className="form-group">
                <label>Start date</label>
                <input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} />
              </div>

              {mode === "date-range" && (
                <div className="form-group">
                  <label>End date</label>
                  <input type="date" value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} />
                </div>
              )}

              {mode === "custom-hours" && (
                <div className="form-row">
                  <div className="form-group">
                    <label>Start time</label>
                    <input type="time" value={form.startTime} onChange={(event) => setForm({ ...form, startTime: event.target.value })} />
                  </div>
                  <div className="form-group">
                    <label>End time</label>
                    <input type="time" value={form.endTime} onChange={(event) => setForm({ ...form, endTime: event.target.value })} />
                  </div>
                </div>
              )}

              <button className="primary-button">Add day off</button>
            </form>

            <div className="section">
              <h3>Days off and breaks</h3>
              {unavailableDates.length === 0 && <p className="muted">No days off yet.</p>}
              <div className="small-list">
                {unavailableDates.map((period) => (
                  <div className="small-list-row" key={period.id}>
                    <span>{formatDateTime(period.startDateTime)}</span>
                    <span>{formatDateTime(period.endDateTime)}</span>
                    <button className="danger-button compact-button" type="button" onClick={() => handleDeleteUnavailable(period.id)}>Delete</button>
                  </div>
                ))}
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

export default ArtistSchedulePage;
