
import { useState } from "react";
import { createTattooRequest } from "../api/tattooRequestApi";
import "../styles/tattooRequest.css";

function CreateTattooRequestPage() {
  const [form, setForm] = useState({
    tattooArtistId: "",
    description: "",
    placement: "",
    images: [""],
  });

  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleChange(event) {
    setForm({
      ...form,
      [event.target.name]: event.target.value,
    });
  }

  function handleImageChange(index, value) {
    const updatedImages = [...form.images];
    updatedImages[index] = value;

    setForm({
      ...form,
      images: updatedImages,
    });
  }

  function addImageInput() {
    setForm({
      ...form,
      images: [...form.images, ""],
    });
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setSuccessMessage("");

    const requestData = {
      tattooArtistId: Number(form.tattooArtistId),
      description: form.description,
      placement: form.placement,
      images: form.images
        .filter((imageUrl) => imageUrl.trim() !== "")
        .map((imageUrl) => ({
          imageUrl: imageUrl,
        })),
    };

    try {
      const response = await createTattooRequest(requestData);

      if (!response.ok) {
        const errorText = await response.text();
        setError(errorText || "Failed to create tattoo request.");
        return;
      }

      setSuccessMessage("Tattoo request created successfully.");

      setForm({
        tattooArtistId: "",
        description: "",
        placement: "",
        images: [""],
      });
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="tattoo-request-page">
      <section className="tattoo-request-card">
        <div className="tattoo-request-header">
          <p className="tattoo-request-subtitle">Tattoo Request</p>
          <h1>Create tattoo request</h1>
          <p>
            Describe your tattoo idea, placement, and add reference image URLs
            so the artist can review your request.
          </p>
        </div>

        <form className="tattoo-request-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="tattooArtistId">Tattoo artist ID</label>
            <input
              id="tattooArtistId"
              name="tattooArtistId"
              type="number"
              placeholder="Example: 1"
              value={form.tattooArtistId}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="description">Tattoo description</label>
            <textarea
              id="description"
              name="description"
              placeholder="Describe your tattoo idea, style, size, details..."
              value={form.description}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="placement">Placement</label>
            <input
              id="placement"
              name="placement"
              type="text"
              placeholder="Example: forearm, shoulder, back..."
              value={form.placement}
              onChange={handleChange}
            />
          </div>

          <div className="tattoo-request-section">
            <h2>Reference images</h2>

            {form.images.map((imageUrl, index) => (
              <div className="form-group" key={index}>
                <label>Image URL {index + 1}</label>
                <input
                  type="text"
                  placeholder="Paste reference image URL"
                  value={imageUrl}
                  onChange={(event) =>
                    handleImageChange(index, event.target.value)
                  }
                />
              </div>
            ))}

            <button
              type="button"
              className="secondary-button"
              onClick={addImageInput}
            >
              Add Image
            </button>
          </div>

          {error && <p className="tattoo-request-error">{error}</p>}

          {successMessage && (
            <p className="tattoo-request-success">{successMessage}</p>
          )}

          <button type="submit" className="tattoo-request-submit-button">
            Create Tattoo Request
          </button>
        </form>
      </section>
    </main>
  );
}

export default CreateTattooRequestPage;