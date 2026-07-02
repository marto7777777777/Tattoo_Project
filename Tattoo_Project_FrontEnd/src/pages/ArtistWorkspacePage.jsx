import { Link } from "react-router-dom";

function ArtistWorkspacePage() {
  return (
    <main className="page-shell"><section className="container"><div className="header"><p className="subtitle">Artist Workspace</p><h1>Manage artist workflow</h1><p>Use these tools to respond to requests, complete consultations, add sessions, and complete tattoos.</p></div>
      <div className="grid-2">
        <article className="card form-card"><h2>Respond to request</h2><p className="muted">Accept a tattoo request by creating an artist response with estimate and message.</p><Link className="primary-button" to="/artist-response">Create Artist Response</Link></article>
        <article className="card form-card"><h2>Complete consultation</h2><p className="muted">After a consultation, define session count, prices, and duration hours.</p><Link className="primary-button" to="/complete-consultation">Complete Consultation</Link></article>
        <article className="card form-card"><h2>Add more sessions</h2><p className="muted">Add additional planned tattoo sessions with price and duration.</p><Link className="primary-button" to="/add-more-sessions">Add More Sessions</Link></article>
        <article className="card form-card"><h2>Complete tattoo</h2><p className="muted">Mark the tattoo request as completed when all planned sessions are booked.</p><Link className="primary-button" to="/complete-tattoo">Complete Tattoo</Link></article>
      </div>
      <p className="muted backend-note">Backend note: there is no dedicated endpoint for "my assigned tattoo requests" yet. Add one later for a full artist inbox page.</p>
    </section></main>
  );
}
export default ArtistWorkspacePage;
