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
import { getStatusClass, getStatusName } from "../utils/format";
import { WeeklyScheduleBuilder } from "../components/WeeklyScheduleBuilder";
import { getUiLocale } from "../i18n/locale";

const EVENT_FILTERS = [
  { label: "All", value: "all" },
  { label: "Consultations", value: "consultation" },
  { label: "Tattoo sessions", value: "tattoo-session" },
  { label: "Days off", value: "unavailable" },
];

function schedulesToAvailability(schedules = []) {
  const build = (type, prefix, fallback) => {
    const grouped = new Map();
    schedules.filter((item) => Number(item.scheduleType) === type).forEach((item) => {
      const startTime = String(item.startTime).slice(0, 5);
      const endTime = String(item.endTime).slice(0, 5);
      const key = `${startTime}-${endTime}`;
      if (!grouped.has(key)) grouped.set(key, { id: `${prefix}-${grouped.size}-${Date.now()}`, days: [], startTime, endTime });
      grouped.get(key).days.push(Number(item.dayOfWeek));
    });
    return grouped.size ? [...grouped.values()] : [fallback];
  };
  return {
    consultation: build(1, "consultation-edit", { id: "consultation-edit-default", days: [], startTime: "10:00", endTime: "12:00" }),
    tattoo: build(0, "tattoo-edit", { id: "tattoo-edit-default", days: [], startTime: "13:00", endTime: "18:00" }),
  };
}
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
  return date.toLocaleDateString(getUiLocale(), { month: "long", year: "numeric" });
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

function isValidDate(date) {
  return date instanceof Date && !Number.isNaN(date.getTime());
}

function resolveEventEndTime(start, end, duration, durationUnit = "minutes") {
  const startDate = new Date(start);
  const endDate = new Date(end);

  if (isValidDate(endDate) && endDate > startDate) {
    return end;
  }

  const numericDuration = Number(duration);
  if (!isValidDate(startDate) || !Number.isFinite(numericDuration) || numericDuration <= 0) {
    return null;
  }

  const durationInMilliseconds = numericDuration
    * (durationUnit === "hours" ? 60 : 1)
    * 60
    * 1000;

  return new Date(startDate.getTime() + durationInMilliseconds).toISOString();
}

function formatTime(value) {
  const date = new Date(value);
  if (!isValidDate(date)) return "";

  return date.toLocaleTimeString(getUiLocale(), {
    hour: "2-digit",
    minute: "2-digit",
  });
}

function getEventTimeRange(start, end) {
  const startLabel = formatTime(start);
  const endLabel = formatTime(end);

  if (!startLabel) return "Time unavailable";
  return endLabel ? `${startLabel} – ${endLabel}` : `Starts at ${startLabel}`;
}

function isStartOfDay(date) {
  return date.getHours() === 0
    && date.getMinutes() === 0
    && date.getSeconds() === 0
    && date.getMilliseconds() === 0;
}

function formatCalendarDate(date) {
  return date.toLocaleDateString(getUiLocale(), {
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function formatDateInputValue(value) {
  if (!value) return "Choose date";
  const date = new Date(`${value}T00:00:00`);
  return isValidDate(date) ? formatCalendarDate(date) : "Choose date";
}

function formatTimeInputValue(value) {
  if (!value) return "Choose time";
  const [hours, minutes] = String(value).split(":");
  return `${String(hours || "00").padStart(2, "0")}:${String(minutes || "00").padStart(2, "0")}`;
}

function periodBlocksEntireDay(period, dateKey) {
  const periodStart = new Date(period.startDateTime);
  const periodEnd = new Date(period.endDateTime);
  const dayStart = new Date(`${dateKey}T00:00:00`);
  const dayEnd = new Date(dayStart);
  dayEnd.setDate(dayEnd.getDate() + 1);

  return isValidDate(periodStart)
    && isValidDate(periodEnd)
    && periodStart <= dayStart
    && periodEnd >= dayEnd;
}

function describeUnavailablePeriod(period) {
  const start = new Date(period.startDateTime);
  const end = new Date(period.endDateTime);

  if (!isValidDate(start) || !isValidDate(end) || end <= start) {
    return {
      typeLabel: "Unavailable",
      title: "Unavailable period",
      detail: "Date information unavailable",
      blocksFullDay: false,
    };
  }

  if (isStartOfDay(start) && isStartOfDay(end)) {
    const lastIncludedDay = new Date(end);
    lastIncludedDay.setMilliseconds(-1);
    const isSingleDay = toDateKey(start) === toDateKey(lastIncludedDay);

    return isSingleDay
      ? {
        typeLabel: "Full day",
        title: formatCalendarDate(start),
        detail: "Unavailable all day",
        blocksFullDay: true,
      }
      : {
        typeLabel: "Date range",
        title: `${formatCalendarDate(start)} – ${formatCalendarDate(lastIncludedDay)}`,
        detail: `${Math.round((end - start) / 86400000)} days unavailable`,
        blocksFullDay: true,
      };
  }

  const sameDay = toDateKey(start) === toDateKey(end);
  return {
    typeLabel: "Hourly break",
    title: sameDay
      ? formatCalendarDate(start)
      : `${formatCalendarDate(start)} – ${formatCalendarDate(end)}`,
    detail: `${formatTime(start)} – ${formatTime(end)}`,
    blocksFullDay: false,
  };
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
  const [scheduleForm, setScheduleForm] = useState({ consultation: [], tattoo: [] });

  useEffect(() => {
    loadCalendarData();
    loadProfile();
  }, []);

  async function loadProfile() {
    try {
      const data = await getMyProfile();
      setProfile(data);
      setScheduleForm(schedulesToAvailability(data.artist?.schedules || []));
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
        const consultationEndTime = resolveEventEndTime(
          request.consultation.startTime,
          request.consultation.endTime,
          profile?.artist?.consultationDurationMinutes || 30,
        );

        addEvent(getEventDateKey(request.consultation.startTime), {
          type: "consultation",
          label: "Consultation",
          title: request.placement || "Consultation",
          startTime: request.consultation.startTime,
          endTime: consultationEndTime,
          requestId: request.id,
          request,
        });
      }

      request.tattooSessions?.forEach((session, index) => {
        const sessionEndTime = resolveEventEndTime(
          session.startTime,
          session.endTime,
          session.durationHours,
          "hours",
        );

        addEvent(getEventDateKey(session.startTime), {
          type: "tattoo-session",
          label: "Tattoo session",
          title: `${request.placement || "Tattoo session"} · Session ${index + 1}`,
          startTime: session.startTime,
          endTime: sessionEndTime,
          requestId: request.id,
          request,
        });
      });
    });

    unavailableDates.forEach((period) => {
      const start = new Date(period.startDateTime);
      const end = new Date(period.endDateTime);
      const cursor = new Date(start.getFullYear(), start.getMonth(), start.getDate());
      const description = describeUnavailablePeriod(period);

      // EndDateTime is an exclusive boundary. A full day saved as
      // 15 Jul 00:00 -> 16 Jul 00:00 must therefore mark only 15 July.
      // Comparing the day cursor with the real end time also keeps custom
      // hourly breaks visible on their start day.
      while (cursor < end) {
        addEvent(toDateKey(cursor), {
          type: "unavailable",
          label: description.typeLabel,
          title: description.title,
          timeLabel: description.detail,
          startTime: period.startDateTime,
          endTime: period.endDateTime,
          unavailableId: period.id,
          blocksFullDay: description.blocksFullDay,
        });
        cursor.setDate(cursor.getDate() + 1);
      }
    });

    Object.values(result).forEach((events) => {
      events.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
    });

    return result;
  }, [requests, unavailableDates, profile]);

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

  async function handleUpdateSchedule(event) {
    event.preventDefault();
    if (!profile?.artist) {
      setError("Artist profile was not loaded yet.");
      return;
    }

    setError("");
    setSuccess("");

    const mapBlocks = (blocks, scheduleType) => blocks.flatMap((block) => block.days.map((day) => ({
      dayOfWeek: day, startTime: `${block.startTime}:00`, endTime: `${block.endTime}:00`, scheduleType,
    })));
    const schedules = [...mapBlocks(scheduleForm.consultation, 1), ...mapBlocks(scheduleForm.tattoo, 0)];

    const duplicateKeys = new Set();
    for (const schedule of schedules) {
      const key = `${schedule.dayOfWeek}-${schedule.startTime}-${schedule.endTime}-${schedule.scheduleType}`;
      if (duplicateKeys.has(key)) {
        setError("The schedule contains a duplicate time block.");
        return;
      }
      duplicateKeys.add(key);
    }

    if ([...scheduleForm.consultation, ...scheduleForm.tattoo].some((block) => block.days.length && block.startTime >= block.endTime)) {
      setError("The end time must be later than the start time.");
      return;
    }

    for (const day of [0, 1, 2, 3, 4, 5, 6]) {
      const daySchedules = schedules.filter((schedule) => schedule.dayOfWeek === day).sort((a, b) => a.startTime.localeCompare(b.startTime));
      for (let i = 1; i < daySchedules.length; i += 1) {
        if (daySchedules[i].startTime < daySchedules[i - 1].endTime) {
          setError("Working hours cannot overlap on the same day. Consultations and tattoo sessions cannot overlap either.");
          return;
        }
      }
    }

    if (!schedules.some((schedule) => schedule.scheduleType === 1) || !schedules.some((schedule) => schedule.scheduleType === 0)) {
      setError("Add at least one consultation schedule and one tattoo session schedule.");
      return;
    }

    const payload = {
      firstName: profile.firstName,
      lastName: profile.lastName,
      email: profile.email,
      description: profile.artist.description,
      consultationDurationMinutes: profile.artist.consultationDurationMinutes,
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
          <form className="card form-card onboarding-section schedule-builder-section section" onSubmit={handleUpdateSchedule}>
            <div className="onboarding-section-head"><div><p className="subtitle">Working availability</p><h2>Edit working schedule</h2><p className="muted">Use the same simple schedule builder as profile creation.</p></div></div>
            <WeeklyScheduleBuilder value={scheduleForm} onChange={setScheduleForm} />
            <div className="action-row" style={{ marginTop: 18 }}><button className="primary-button">Save schedule</button></div>
          </form>
        )}

        <div className="filter-tabs">
          {EVENT_FILTERS.map((filter) => (
            <button
              key={filter.value}
              className={`filter-tab ${eventFilter === filter.value ? "filter-tab-active" : ""}`}
              type="button"
              aria-pressed={eventFilter === filter.value}
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
              <span><i className="event-dot event-unavailable" /> Day off / break</span>
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
                const hasFullDayOff = unavailableDates.some((period) => periodBlocksEntireDay(period, key));
                const isOutsideSchedule = Boolean(profile?.artist) && !scheduledWeekdays.has(day.getDay());
                const isEntireDayUnavailable = hasFullDayOff || isOutsideSchedule;

                return (
                  <button
                    className={`calendar-day ${isCurrentMonth ? "" : "calendar-day-muted"} ${isSelected ? "calendar-day-selected" : ""} ${isEntireDayUnavailable ? "calendar-day-off" : ""}`}
                    key={key}
                    type="button"
                    aria-pressed={isSelected}
                    onClick={() => {
                      setSelectedDateKey(key);
                      setForm((current) => ({ ...current, date: key, endDate: key }));
                    }}
                  >
                    <span className="calendar-day-top">
                      <span className="calendar-day-number">{day.getDate()}</span>
                      {isEntireDayUnavailable && <span className="calendar-day-off-label">OFF</span>}
                    </span>
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
            <h2>{new Date(`${selectedDateKey}T00:00:00`).toLocaleDateString(getUiLocale())}</h2>

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
                      <p className="muted">{event.timeLabel || getEventTimeRange(event.startTime, event.endTime)}</p>
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
                <div className="calendar-picker-shell">
                  <span>{formatDateInputValue(form.date)}</span>
                  <input
                    aria-label="Start date"
                    type="date"
                    value={form.date}
                    onChange={(event) => setForm({ ...form, date: event.target.value })}
                  />
                </div>
              </div>

              {mode === "date-range" && (
                <div className="form-group">
                  <label>End date</label>
                  <div className="calendar-picker-shell">
                    <span>{formatDateInputValue(form.endDate)}</span>
                    <input
                      aria-label="End date"
                      type="date"
                      value={form.endDate}
                      onChange={(event) => setForm({ ...form, endDate: event.target.value })}
                    />
                  </div>
                </div>
              )}

              {mode === "custom-hours" && (
                <div className="form-row">
                  <div className="form-group">
                    <label>Start time</label>
                    <div className="calendar-picker-shell">
                      <span>{formatTimeInputValue(form.startTime)}</span>
                      <input
                        aria-label="Start time"
                        type="time"
                        value={form.startTime}
                        onChange={(event) => setForm({ ...form, startTime: event.target.value })}
                      />
                    </div>
                  </div>
                  <div className="form-group">
                    <label>End time</label>
                    <div className="calendar-picker-shell">
                      <span>{formatTimeInputValue(form.endTime)}</span>
                      <input
                        aria-label="End time"
                        type="time"
                        value={form.endTime}
                        onChange={(event) => setForm({ ...form, endTime: event.target.value })}
                      />
                    </div>
                  </div>
                </div>
              )}

              <button className="primary-button">Add day off</button>
            </form>

            <div className="section">
              <h3>Days off and breaks</h3>
              {unavailableDates.length === 0 && <p className="muted">No days off yet.</p>}
              <div className="unavailable-period-list">
                {unavailableDates.map((period) => {
                  const description = describeUnavailablePeriod(period);
                  return (
                    <article className="unavailable-period-card" key={period.id}>
                      <div className="unavailable-period-copy">
                        <span className="unavailable-period-type">{description.typeLabel}</span>
                        <strong>{description.title}</strong>
                        <span className="muted">{description.detail}</span>
                      </div>
                      <button className="danger-button compact-button" type="button" onClick={() => handleDeleteUnavailable(period.id)}>Delete</button>
                    </article>
                  );
                })}
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

export default ArtistSchedulePage;
