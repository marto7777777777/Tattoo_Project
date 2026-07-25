export function toLocalDateValue(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function todayDateValue() {
  return toLocalDateValue(new Date());
}

export function shiftDateValue(value, days) {
  const date = new Date(`${value}T00:00:00`);
  date.setDate(date.getDate() + days);
  return toLocalDateValue(date);
}

export function formatBookingPeriod(startDate, periodDays) {
  const start = new Date(`${startDate}T00:00:00`);
  const end = new Date(start);
  end.setDate(end.getDate() + periodDays - 1);
  const formatter = new Intl.DateTimeFormat(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
  return `${formatter.format(start)} – ${formatter.format(end)}`;
}
