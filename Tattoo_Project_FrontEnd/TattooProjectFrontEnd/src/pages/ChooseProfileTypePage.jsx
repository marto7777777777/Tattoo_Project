
import "../styles/profileSetup.css";

function ChooseProfileTypePage() {
  return (
    <main className="profile-setup-page">
      <section className="profile-setup-container">
        <div className="profile-setup-header">
          <p className="profile-setup-subtitle">Profile Setup</p>
          <h1>How do you want to use the platform?</h1>
          <p>
            Choose the type of profile you want to create. You can continue as
            a client looking for tattoo artists or as a tattoo artist offering
            services.
          </p>
        </div>

        <div className="profile-choice-grid">
          <article className="profile-choice-card">
            <div className="profile-choice-icon">🖤</div>
            <h2>Client</h2>
            <p>
              Create tattoo requests, browse tattoo artists, book consultations,
              and manage your tattoo sessions.
            </p>

            <button className="profile-choice-button">
              Continue as Client
            </button>
          </article>

          <article className="profile-choice-card artist-card">
            <div className="profile-choice-icon">✒️</div>
            <h2>Tattoo Artist</h2>
            <p>
              Create an artist profile, show your studio information, add
              portfolio images, requirements, and working schedule.
            </p>

            <button className="profile-choice-button">
              Continue as Artist
            </button>
          </article>
        </div>
      </section>
    </main>
  );
}

export default ChooseProfileTypePage;