import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { createTattooRequest } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";

function CreateTattooRequestPage() {
  const navigate = useNavigate();
  const params = useParams();
  const storedArtist = JSON.parse(localStorage.getItem("selectedArtist") || "null");
  const artistId = params.artistId || storedArtist?.id || storedArtist?.tattooArtistId || "";
  const [form, setForm] = useState({ description: "", placement: "", images: [""] });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value });
  }

  function updateImage(index, value) {
    const copy = [...form.images];
    copy[index] = value;
    setForm({ ...form, images: copy });
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!artistId) {
      setError("Please choose an artist before creating a tattoo request.");
      return;
    }

    const requestData = {
      tattooArtistId: Number(artistId),
      description: form.description,
      placement: form.placement,
      images: form.images
        .filter((imageUrl) => imageUrl.trim())
        .map((imageUrl) => ({ imageUrl })),
    };

    try {
      const response = await createTattooRequest(requestData);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Tattoo request created successfully.");
      setTimeout(() => navigate("/bookings"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  if (!artistId) {
    return (
      <main className="center-container">
        <section className="card form-card">
          <div className="header">
            <p className="subtitle">Tattoo Request</p>
            <h1>Choose an artist first</h1>
            <p>Open the Artists page and select the artist you want to contact.</p>
          </div>

          <Link className="primary-button" to="/explore">
            Browse Artists
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="center-container">
      <section className="request-layout">
        <aside className="card side-panel">
          <p className="subtitle">Selected Artist</p>
          <h2>{storedArtist?.studioName || "Artist selected"}</h2>
          <p className="muted">
            {storedArtist
              ? `${storedArtist.firstName} ${storedArtist.lastName}`
              : "You selected this artist from the Artists page."}
          </p>

          <div className="info-list">
            {storedArtist?.studioAddress && <p><span>Studio:</span> {storedArtist.studioAddress}</p>}
            {(storedArtist?.studioCity || storedArtist?.studioCountry) && (
              <p><span>Location:</span> {[storedArtist.studioCity, storedArtist.studioCountry].filter(Boolean).join(", ")}</p>
            )}
            {storedArtist?.consultationDurationMinutes && (
              <p><span>Consultation:</span> {storedArtist.consultationDurationMinutes} minutes</p>
            )}
          </div>
        </aside>

        <section className="card form-card">
          <div className="header">
            <p className="subtitle">Tattoo Request</p>
            <h1>Describe your tattoo idea</h1>
            <p>Add details, placement, and reference image URLs.</p>
          </div>

          <form className="form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Description</label>
              <textarea name="description" value={form.description} onChange={handleChange} />
            </div>

            <div className="form-group">
              <label>Placement</label>
              <input name="placement" value={form.placement} onChange={handleChange} />
            </div>

            <div className="section">
              <h2>Reference images</h2>
              {form.images.map((imageUrl, index) => (
                <div className="form-group" key={index}>
                  <label>Image URL {index + 1}</label>
                  <input value={imageUrl} onChange={(e) => updateImage(index, e.target.value)} />
                </div>
              ))}
              <button
                type="button"
                className="secondary-button"
                onClick={() => setForm({ ...form, images: [...form.images, ""] })}
              >
                Add image
              </button>
            </div>

            {error && <p className="error">{error}</p>}
            {success && <p className="success">{success}</p>}

            <button className="primary-button">Send Tattoo Request</button>
          </form>
        </section>
      </section>
    </main>
  );
}

export default CreateTattooRequestPage;
