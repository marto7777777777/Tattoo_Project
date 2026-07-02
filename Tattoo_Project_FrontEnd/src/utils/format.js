export const REQUEST_STATUS = {
  0: "Submitted",
  1: "Under review",
  2: "Approved",
  3: "Waiting for consultation",
  4: "Consultation completed",
  5: "Tattoo booked",
  6: "In progress",
  7: "Completed",
  8: "Rejected",
};

export function getStatusName(status) {
  return REQUEST_STATUS[status] || "Unknown";
}

export function getStatusClass(status) {
  const classes = {
    0: "status-submitted",
    1: "status-waiting",
    2: "status-approved",
    3: "status-waiting",
    4: "status-consultation-completed",
    5: "status-booked",
    6: "status-progress",
    7: "status-completed",
    8: "status-rejected",
  };

  return classes[status] || "";
}

export function getDayName(dayOfWeek) {
  const days = {
    0: "Sunday",
    1: "Monday",
    2: "Tuesday",
    3: "Wednesday",
    4: "Thursday",
    5: "Friday",
    6: "Saturday",
  };

  return days[dayOfWeek] || "Unknown day";
}

export function getScheduleTypeName(scheduleType) {
  if (scheduleType === 1) return "Consultation";
  if (scheduleType === 0) return "Tattoo session";
  return "Schedule";
}

export function formatTime(time) {
  if (!time) return "";
  if (typeof time === "string" && time.includes(":")) return time.slice(0, 5);

  return new Date(time).toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDate(value) {
  if (!value) return "Unknown date";
  return new Date(value).toLocaleDateString();
}

export function formatDateTime(value) {
  if (!value) return "";
  return new Date(value).toLocaleString();
}

export function toApiDateTime(localDateTimeValue) {
  if (!localDateTimeValue) return "";
  return new Date(localDateTimeValue).toISOString();
}

export function getEntityId(entity, index) {
  return entity?.id || entity?.tattooRequestId || entity?.tattooArtistId || index + 1;
}
