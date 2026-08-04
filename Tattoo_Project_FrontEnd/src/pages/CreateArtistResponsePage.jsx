import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createArtistResponse } from "../api/artistResponseApi";
import { readResponse } from "../api/http";
import ArtistResponseWorkflowFields, {
  buildArtistResponsePayload,
  createArtistResponseForm,
} from "../components/ArtistResponseWorkflowFields";

function CreateArtistResponsePage() {
  const params = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    ...createArtistResponseForm(),
    tattooRequestId: params.tattooRequestId || "",
  });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(event) {
    setForm({ ...form, [event.target.name]: event.target.value });
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (form.workflowPath == null) {
      setError("Choose how the tattoo project should continue.");
      return;
    }

    try {
      const response = await createArtistResponse(buildArtistResponsePayload(form, form.tattooRequestId));
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Artist response created successfully.");
      setTimeout(() => navigate("/my-studio"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="center-container artist-action-page">
      <section className="card form-card artist-action-card">
        <div className="header">
          <p className="subtitle">Artist Response</p>
          <h1>Respond to tattoo request</h1>
          <p>Create an acceptance response for the tattoo request.</p>
        </div>

        <form className="form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Tattoo request ID</label>
            <input
              name="tattooRequestId"
              type="number"
              value={form.tattooRequestId}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label>Response message</label>
            <textarea
              name="responseMessage"
              value={form.responseMessage}
              onChange={handleChange}
            />
          </div>

          <ArtistResponseWorkflowFields form={form} setForm={setForm} />

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}

          <div className="action-row">
            <button className="primary-button">Create Response</button>
          </div>
        </form>
      </section>
    </main>
  );
}

export default CreateArtistResponsePage;
