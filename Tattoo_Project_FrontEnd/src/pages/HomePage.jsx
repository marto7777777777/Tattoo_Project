import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function HomePage() {
  const { isLoggedIn, isClient, isArtist } = useAuth();

  return (
    <main className="page-shell">
      <section className="container home-hero">
        <div className="header">
          <p className="subtitle">Tattoo Booking Platform</p>
          <h1>Manage tattoo requests, consultations, and sessions</h1>
          <p>
            A complete client and tattoo artist workflow for creating requests,
            responding to ideas, booking consultations, and scheduling tattoo sessions.
          </p>
        </div>

        <div className="grid-2">
          <div className="card form-card">
            <h2>For clients</h2>
            <p className="muted">Explore artists, send tattoo requests, book consultations, and schedule approved tattoo sessions.</p>
            <div className="home-actions">
              <Link className="primary-button" to="/explore">Explore artists</Link>
              {isClient && <Link className="secondary-button" to="/bookings">Bookings</Link>}
            </div>
          </div>

          <div className="card form-card">
            <h2>For artists</h2>
            <p className="muted">Set working schedules, answer tattoo requests, complete consultations, and manage sessions.</p>
            <div className="home-actions">
              {isArtist ? (
                <Link className="primary-button" to="/my-studio">Open My Studio</Link>
              ) : isLoggedIn ? (
                <Link className="primary-button" to="/create-artist-profile">Create artist profile</Link>
              ) : (
                <Link className="primary-button" to="/login">Login</Link>
              )}
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}

export default HomePage;
