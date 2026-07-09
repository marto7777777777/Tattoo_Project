import { Link } from "react-router-dom";

function ArtistWorkspacePage() {
  return (
    <main className="page-shell">
      <section className="container">
        <div className="header">
          <p className="subtitle">My Studio</p>
          <h1>Manage your tattoo studio</h1>
          <p>
            Handle incoming tattoo requests and manage your calendar from one clear studio area.
          </p>
        </div>

        <div className="filter-tabs studio-tabs">
          <Link className="filter-tab" to="/my-studio/requests">
            Requests
          </Link>
          <Link className="filter-tab" to="/my-studio/calendar">
            Calendar
          </Link>
        </div>

        <div className="grid-2">
          <article className="card form-card dashboard-card">
            <div>
              <p className="subtitle inline-subtitle">Workflow</p>
              <h2>Requests</h2>
              <p className="muted">
                Review new requests, send responses, manage consultations, add sessions,
                and complete tattoo projects from the request itself.
              </p>
            </div>
            <Link className="primary-button" to="/my-studio/requests">
              Open Requests
            </Link>
          </article>

          <article className="card form-card dashboard-card">
            <div>
              <p className="subtitle inline-subtitle">Calendar</p>
              <h2>Calendar</h2>
              <p className="muted">
                View consultations, tattoo sessions, and days off. Filter what you see and
                add unavailable periods directly from the calendar.
              </p>
            </div>
            <Link className="primary-button" to="/my-studio/calendar">
              Open calendar
            </Link>
          </article>
        </div>
      </section>
    </main>
  );
}

export default ArtistWorkspacePage;
