const DAYS = [
  { value: 1, short: "Mon", label: "Monday" },
  { value: 2, short: "Tue", label: "Tuesday" },
  { value: 3, short: "Wed", label: "Wednesday" },
  { value: 4, short: "Thu", label: "Thursday" },
  { value: 5, short: "Fri", label: "Friday" },
  { value: 6, short: "Sat", label: "Saturday" },
  { value: 0, short: "Sun", label: "Sunday" },
];

export const TIME_OPTIONS = [
  ...Array.from({ length: 96 }, (_, index) => {
    const totalMinutes = index * 15;
    const hours = String(Math.floor(totalMinutes / 60)).padStart(2, "0");
    const minutes = String(totalMinutes % 60).padStart(2, "0");
    return `${hours}:${minutes}`;
  }),
  "23:59",
];

export function formatScheduleTime(value) {
  const [hourText, minuteText] = String(value).split(":");
  const hour = Number(hourText);
  const suffix = hour < 12 ? "AM" : "PM";
  const displayHour = hour % 12 || 12;
  return `${value} · ${displayHour}:${minuteText} ${suffix}`;
}

function createBlock(kind) {
  return {
    id: `${kind}-${Date.now()}-${Math.random().toString(16).slice(2)}`,
    days: [],
    startTime: kind === "consultation" ? "10:00" : "13:00",
    endTime: kind === "consultation" ? "12:00" : "18:00",
  };
}

export function WeeklyScheduleBuilder({ value, onChange }) {
  function updateBlock(kind, blockId, patch) {
    onChange((current) => ({
      ...current,
      [kind]: current[kind].map((block) =>
        block.id === blockId ? { ...block, ...patch } : block
      ),
    }));
  }

  function toggleDay(kind, blockId, day) {
    onChange((current) => ({
      ...current,
      [kind]: current[kind].map((block) => {
        if (block.id !== blockId) return block;
        const isSelected = block.days.includes(day);
        return {
          ...block,
          days: isSelected
            ? block.days.filter((currentDay) => currentDay !== day)
            : [...block.days, day],
        };
      }),
    }));
  }

  function applyPreset(kind, blockId, preset) {
    const presets = {
      weekdays: [1, 2, 3, 4, 5],
      weekend: [6, 0],
      everyDay: [1, 2, 3, 4, 5, 6, 0],
      clear: [],
    };
    updateBlock(kind, blockId, { days: presets[preset] ?? [] });
  }

  function addBlock(kind) {
    onChange((current) => ({
      ...current,
      [kind]: [...current[kind], createBlock(kind)],
    }));
  }

  function removeBlock(kind, blockId) {
    onChange((current) => ({
      ...current,
      [kind]: current[kind].filter((block) => block.id !== blockId),
    }));
  }

  function ScheduleBlock({ kind, block, index }) {
    const isConsultation = kind === "consultation";
    return (
      <div className="availability-block">
        <div className="availability-block-topline">
          <div>
            <strong>
              {isConsultation ? "Consultation hours" : "Tattoo session hours"}
              {index > 0 ? ` #${index + 1}` : ""}
            </strong>
            <span>Select the days that use these exact hours.</span>
          </div>
          {value[kind].length > 1 && (
            <button type="button" className="availability-remove" onClick={() => removeBlock(kind, block.id)}>
              Remove
            </button>
          )}
        </div>

        <div className="schedule-preset-row">
          <button type="button" onClick={() => applyPreset(kind, block.id, "weekdays")}>Mon–Fri</button>
          <button type="button" onClick={() => applyPreset(kind, block.id, "weekend")}>Weekend</button>
          <button type="button" onClick={() => applyPreset(kind, block.id, "everyDay")}>Every day</button>
          <button type="button" onClick={() => applyPreset(kind, block.id, "clear")}>Clear</button>
        </div>

        <div className="schedule-day-grid" aria-label={`${isConsultation ? "Consultation" : "Tattoo"} working days`}>
          {DAYS.map((day) => {
            const active = block.days.includes(day.value);
            return (
              <button
                type="button"
                key={day.value}
                className={`schedule-day-chip ${active ? "active" : ""}`}
                onClick={() => toggleDay(kind, block.id, day.value)}
                title={day.label}
              >
                <span>{day.short}</span>
                <small>{active ? "On" : "Off"}</small>
              </button>
            );
          })}
        </div>

        <div className="availability-time-row">
          <label>
            <span>From</span>
            <select value={block.startTime} onChange={(event) => updateBlock(kind, block.id, { startTime: event.target.value })}>
              {TIME_OPTIONS.map((time) => <option key={time} value={time}>{formatScheduleTime(time)}</option>)}
            </select>
          </label>
          <span className="availability-time-arrow">→</span>
          <label>
            <span>To</span>
            <select value={block.endTime} onChange={(event) => updateBlock(kind, block.id, { endTime: event.target.value })}>
              {TIME_OPTIONS.map((time) => <option key={time} value={time}>{formatScheduleTime(time)}</option>)}
            </select>
          </label>
        </div>
      </div>
    );
  }

  return (
    <div className="schedule-builder-grid">
      <div className="schedule-builder-column consultation-column">
        <div className="schedule-builder-title">
          <span className="schedule-builder-dot consultation-dot" />
          <div><h3>Consultations</h3><p>When clients can book consultation appointments.</p></div>
        </div>
        {value.consultation.map((block, index) => <ScheduleBlock key={block.id} kind="consultation" block={block} index={index} />)}
        <button type="button" className="schedule-add-block" onClick={() => addBlock("consultation")}>＋ Add different consultation hours</button>
      </div>

      <div className="schedule-builder-column tattoo-column">
        <div className="schedule-builder-title">
          <span className="schedule-builder-dot tattoo-dot" />
          <div><h3>Tattoo sessions</h3><p>When clients can book actual tattoo sessions.</p></div>
        </div>
        {value.tattoo.map((block, index) => <ScheduleBlock key={block.id} kind="tattoo" block={block} index={index} />)}
        <button type="button" className="schedule-add-block" onClick={() => addBlock("tattoo")}>＋ Add different tattoo hours</button>
      </div>
    </div>
  );
}
