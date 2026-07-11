const STATUS = {
  SUBMITTED: 0,
  WAITING_FOR_CONSULTATION: 3,
  CONSULTATION_COMPLETED: 4,
  TATTOO_BOOKED: 5,
  IN_PROGRESS: 6,
  COMPLETED: 7,
  REJECTED: 8,
};

function getStepState(request, step) {
  if (request.status === STATUS.REJECTED) {
    return step === "request" ? "completed" : "rejected";
  }

  const hasResponse = Boolean(request.artistResponse);
  const hasConsultation = Boolean(request.consultation);
  const consultationCompleted = Boolean(request.consultation?.isCompleted) || request.status >= STATUS.CONSULTATION_COMPLETED;
  const sessions = request.tattooSessions?.length || 0;
  const completed = request.status === STATUS.COMPLETED;

  const completedMap = {
    request: true,
    response: hasResponse,
    consultationBooked: hasConsultation,
    consultationCompleted,
    sessions: sessions > 0,
    completed,
  };

  if (completedMap[step]) return "completed";

  const order = ["request", "response", "consultationBooked", "consultationCompleted", "sessions", "completed"];
  const firstPending = order.find((item) => !completedMap[item]);
  return firstPending === step ? "current" : "pending";
}

function RequestWorkflowTimeline({ request, compact = false }) {
  const sessions = request.tattooSessions?.length || 0;
  const remaining = request.remainingSessionsToBook;
  const steps = [
    { key: "request", label: "Request submitted" },
    { key: "response", label: "Artist response" },
    { key: "consultationBooked", label: "Consultation booked" },
    { key: "consultationCompleted", label: "Consultation completed" },
    {
      key: "sessions",
      label: sessions > 0 ? `Tattoo sessions (${sessions} booked${remaining != null ? `, ${remaining} remaining` : ""})` : "Tattoo sessions",
    },
    { key: "completed", label: "Tattoo completed" },
  ];

  return (
    <div className={`request-timeline ${compact ? "request-timeline-compact" : ""}`}>
      {steps.map((step) => {
        const state = getStepState(request, step.key);
        return (
          <div className={`timeline-step timeline-step-${state}`} key={step.key}>
            <span className="timeline-marker">{state === "completed" ? "✓" : state === "rejected" ? "×" : ""}</span>
            <span>{step.label}</span>
          </div>
        );
      })}
    </div>
  );
}

export default RequestWorkflowTimeline;
