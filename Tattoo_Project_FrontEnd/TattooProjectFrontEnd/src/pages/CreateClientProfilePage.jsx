
import { useState } from "react";
import { createClientProfile } from "../api/clientApi";
import "../styles/profileForm.css";

function CreateClientProfilePage() {
  const [form, setForm] = useState({
    phoneNumber: "",
  });

  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleChange(event) {
    setForm({
      ...form,
      [event.target.name]: event.target.value,
    });
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setSuccessMessage("");

    try {
      const response = await createClientProfile(form);

      if (!response.ok) {
        const errorText = await response.text();
        setError(errorText || "Failed to create client profile.");
        return;
      }

      const data = await response.json();

      if (data.token) {
        localStorage.setItem("token", data.token);
      }

      setSuccessMessage("Client profile created successfully.");

      setForm({
        phoneNumber: "",
      });
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="profile-form-page">
      <section className="profile-form-card">
        <div className="profile-form-header">
          <p className="profile-form-subtitle">Client Profile</p>
          <h1>Create your client profile</h1>
          <p>
            Add your phone number so tattoo artists can contact you about
            consultations and tattoo sessions.
          </p>
        </div>

        <form className="profile-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="phoneNumber">Phone number</label>
            <input
              id="phoneNumber"
              name="phoneNumber"
              type="text"
              placeholder="Enter your phone number"
              value={form.phoneNumber}
              onChange={handleChange}
            />
          </div>

          {error && <p className="profile-error">{error}</p>}
          {successMessage && <p className="profile-success">{successMessage}</p>}

          <button type="submit" className="profile-submit-button">
            Create Client Profile
          </button>
        </form>
      </section>
    </main>
  );
}

export default CreateClientProfilePage;