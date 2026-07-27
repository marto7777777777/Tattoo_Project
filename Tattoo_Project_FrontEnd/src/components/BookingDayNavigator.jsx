import {
  formatBookingPeriod,
  shiftDateValue,
  todayDateValue,
} from "../utils/bookingPeriod";
import { getUiLocale } from "../i18n/locale";

function BookingDayNavigator({
  availability,
  selectedDate,
  onSelectDate,
  periodStart,
  periodDays,
  onPeriodChange,
  loading,
}) {
  const isFirstPeriod = periodStart <= todayDateValue();

  function movePeriod(direction) {
    const nextStart = shiftDateValue(periodStart, direction * periodDays);
    onPeriodChange(
      direction < 0 && nextStart < todayDateValue()
        ? todayDateValue()
        : nextStart
    );
  }

  return (
    <>
      <div className="booking-period-toolbar">
        <button
          className="secondary-button booking-period-arrow"
          type="button"
          disabled={isFirstPeriod || loading}
          aria-label="Previous available days"
          onClick={() => movePeriod(-1)}
        >
          ‹
        </button>
        <div>
          <strong>{formatBookingPeriod(periodStart, periodDays)}</strong>
          <span>{periodDays === 7 ? "One week" : "Two weeks"}</span>
        </div>
        <button
          className="secondary-button booking-period-arrow"
          type="button"
          disabled={loading}
          aria-label="Next available days"
          onClick={() => movePeriod(1)}
        >
          ›
        </button>
      </div>

      <div className="booking-day-grid">
        {availability?.days?.map((day) => (
          <button
            className={`booking-day ${day.slots?.length ? "booking-day-available" : "booking-day-disabled"} ${selectedDate === day.date ? "booking-day-selected" : ""}`}
            aria-pressed={selectedDate === day.date}
            type="button"
            key={day.date}
            disabled={!day.slots?.length}
            onClick={() => onSelectDate(day.date)}
          >
            <strong>
              {new Date(`${day.date}T00:00:00`).toLocaleDateString(getUiLocale(), {
                weekday: "short",
                day: "numeric",
                month: "short",
              })}
            </strong>
            <span>
              {day.slots?.length
                ? `${day.slots.length} free slots`
                : day.reason || "Busy"}
            </span>
          </button>
        ))}
      </div>
    </>
  );
}

export default BookingDayNavigator;
