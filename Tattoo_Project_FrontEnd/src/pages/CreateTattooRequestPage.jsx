import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { createTattooRequestWithImages } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";

function CreateTattooRequestPage() {
  const navigate = useNavigate();
  const params = useParams();
  const storedArtist = JSON.parse(localStorage.getItem("selectedArtist") || "null");
  const artistId = params.artistId || storedArtist?.id || storedArtist?.tattooArtistId || "";
  const [form, setForm] = useState({ description: "", placement: "" });
  const [imageFiles, setImageFiles] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const previews = useMemo(() => imageFiles.map((file) => ({ file, url: URL.createObjectURL(file) })), [imageFiles]);

  function handleChange(e) {
    setForm({ ...form, [e.target.name]: e.target.value });
  }

  function handleImagesChange(event) {
    const files = Array.from(event.target.files || []);
    setImageFiles((current) => [...current, ...files]);
    event.target.value = "";
  }

  function removeImage(index) {
    setImageFiles((current) => current.filter((_, itemIndex) => itemIndex !== index));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!artistId) {
      setError("Please choose an artist before creating a tattoo request.");
      return;
    }

    try {
      const response = await createTattooRequestWithImages(
        {
          tattooArtistId: Number(artistId),
          description: form.description,
          placement: form.placement,
        },
        imageFiles
      );
      const data = await readResponse(response);

      if (!response.ok) {
        const message = typeof data === "string" ? data : JSON.stringify(data);
        if (message.includes("Client profile")) {
          navigate(`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(`/create-tattoo-request/${artistId}`)}`);
          return;
        }
        setError(message);
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
            <p>Open Explore and select the artist you want to contact.</p>
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
              : "You selected this artist from Explore."}
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
            <p>Add details, placement, and upload reference images.</p>
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
              <label className="portfolio-upload-tile">
                <input type="file" accept="image/*" multiple hidden onChange={handleImagesChange} />
                <span>＋</span>
                Upload tattoo reference photos
              </label>

              {previews.length > 0 && (
                <div className="portfolio-manage-grid">
                  {previews.map((preview, index) => (
                    <div className="portfolio-manage-card" key={`${preview.file.name}-${index}`}>
                      <img src={preview.url} alt="Tattoo reference preview" />
                      <button className="danger-button compact-button" type="button" onClick={() => removeImage(index)}>
                        Remove
                      </button>
                    </div>
                  ))}
                </div>
              )}
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
