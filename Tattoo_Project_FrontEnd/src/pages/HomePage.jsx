import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import overviewOne from "../assets/home/home-artist.jpg";
import overviewTwo from "../assets/home/home-studio.jpg";
import overviewThree from "../assets/home/home-equipment.jpg";
import overviewFour from "../assets/home/home-story.jpg";

const overviewSlides = [
  { src: overviewOne, alt: "Tattoo artist working on a client" },
  { src: overviewTwo, alt: "Modern professional tattoo studio" },
  { src: overviewThree, alt: "Tattoo equipment prepared for a session" },
  { src: overviewFour, alt: "Ink your story neon sign inside a tattoo studio" },
];

function HomePage() {
  const { isLoggedIn, isClient, isArtist } = useAuth();
  const [activeSlide, setActiveSlide] = useState(0);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setActiveSlide((current) => (current + 1) % overviewSlides.length);
    }, 3000);
    return () => window.clearInterval(timer);
  }, []);

  return (
    <main className="page-shell home-dashboard">
      <section className="container">
        <div className="dashboard-topline">
          <div>
            <p className="eyebrow-label">InkRoute workspace</p>
            <h1>From first idea to final session.</h1>
            <p className="dashboard-lead">Discover the right artist, shape the concept, book every step and keep the whole tattoo journey in one beautifully organized place.</p>
          </div>

        </div>

        <section className="hero-command-card">
          <div className="hero-command-copy">
            <span className="hero-badge">One workflow. Zero chaos.</span>
            <h2>Your next tattoo deserves more than a DM.</h2>
            <p>Share references, choose placement and style, schedule consultations, manage sessions and always know what happens next.</p>
            <div className="hero-command-actions">
              <Link className="primary-button" to="/explore">Find your artist</Link>
              {isArtist && <Link className="text-link-button" to="/my-studio">Open studio dashboard →</Link>}
            </div>
            <div className="hero-metrics hero-metrics-workflow">
              <div><strong>01</strong><span>Request submitted</span></div>
              <div><strong>02</strong><span>Artist response</span></div>
              <div><strong>03</strong><span>Consultation booked</span></div>
              <div><strong>04</strong><span>Consultation completed</span></div>
              <div><strong>05</strong><span>Tattoo sessions</span></div>
              <div><strong>06</strong><span>Completed</span></div>
            </div>
          </div>

          <div className="hero-visual-panel overview-carousel">
            <img
              key={overviewSlides[activeSlide].src}
              className="overview-slide active"
              src={overviewSlides[activeSlide].src}
              alt={overviewSlides[activeSlide].alt}
            />
            <div className="overview-slide-shade" />
            <div className="overview-dots" aria-label="Overview image controls">
              {overviewSlides.map((slide, index) => (
                <button
                  key={slide.src}
                  type="button"
                  aria-label={`Show image ${index + 1}`}
                  className={index === activeSlide ? "active" : ""}
                  onClick={() => setActiveSlide(index)}
                />
              ))}
            </div>
            <div className="floating-project-card">
              <span className="project-state-dot" />
              <div><small>Current project</small><strong>Fine line botanical</strong><span>Consultation booked · Thu 14:30</span></div>
            </div>
          </div>
        </section>

        <section className="dashboard-grid">
          <article className="dashboard-panel dashboard-panel-large">
            <div className="panel-heading"><div><p className="eyebrow-label">Client journey</p><h3>Everything stays connected</h3></div><span className="panel-number">01</span></div>
            <div className="journey-list">
              <div className="journey-item active"><span>01</span><div><strong>Request submitted</strong><p>Placement, style, description and reference images are sent to the chosen artist.</p></div></div>
              <div className="journey-item"><span>02</span><div><strong>Artist response</strong><p>The artist reviews the idea and responds with the next details.</p></div></div>
              <div className="journey-item"><span>03</span><div><strong>Consultation booked</strong><p>The client chooses an available consultation slot from that artist's schedule.</p></div></div>
              <div className="journey-item"><span>04</span><div><strong>Consultation completed</strong><p>The artist completes the consultation and defines the tattoo-session plan.</p></div></div>
              <div className="journey-item"><span>05</span><div><strong>Tattoo sessions</strong><p>Sessions are booked and the tattoo moves through the in-progress stage.</p></div></div>
              <div className="journey-item"><span>06</span><div><strong>Completed</strong><p>After the final session the tattoo moves into completed work.</p></div></div>
            </div>
          </article>

          <article className="dashboard-panel accent-panel">
            <p className="eyebrow-label">For artists</p>
            <h3>Your studio, organized.</h3>
            <p>Requests, consultations, schedules, sessions and client communication in one focused workspace.</p>
            {isArtist ? <Link to="/my-studio">Go to My Studio →</Link> : isLoggedIn ? <Link to="/create-artist-profile">Create artist profile →</Link> : <Link to="/register">Join as an artist →</Link>}
          </article>

          <article className="dashboard-panel mini-panel">
            <p className="eyebrow-label">Built around clarity</p>
            <div className="mini-stat"><strong>6</strong><span>clear workflow stages</span></div>
            <div className="mini-stat"><strong>1</strong><span>account for client and artist</span></div>
          </article>
        </section>
      </section>
    </main>
  );
}

export default HomePage;
