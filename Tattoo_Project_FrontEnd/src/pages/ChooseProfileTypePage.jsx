import { Link } from "react-router-dom";

function ChooseProfileTypePage() {
  return (
    <main className="page-shell">
      <section className="container">
        <div className="header"><p className="subtitle">Profile setup</p><h1>How do you want to use the platform?</h1><p>Choose whether you want to create a client profile or a tattoo artist profile.</p></div>
        <div className="grid-2">
          <article className="card form-card choice-card"><div className="choice-icon">🖤</div><h2>Client</h2><p className="muted">Create requests, book consultations, and schedule tattoo sessions.</p><Link className="primary-button" to="/create-client-profile">Continue as Client</Link></article>
          <article className="card form-card choice-card"><div className="choice-icon">✒️</div><h2>Tattoo Artist</h2><p className="muted">Create your studio profile, working schedule, portfolio, and requirements.</p><Link className="primary-button" to="/create-artist-profile">Continue as Artist</Link></article>
        </div>
      </section>
    </main>
  );
}
export default ChooseProfileTypePage;
