import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createClientProfile } from "../api/clientApi";
import { updateProfileImage } from "../api/profileApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

function CreateClientProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [form, setForm] = useState({ phoneNumber: "", city: "", country: "" });
  const [profileImageFile, setProfileImageFile] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(event) {
    const { name, value } = event.target;
    setForm({ ...form, [name]: value });
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    try {
      const response = await createClientProfile({
        phoneNumber: form.phoneNumber,
        city: form.city,
        country: form.country,
      });

      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      if (data.token || data.Token) saveAuthToken(data.token || data.Token);

      if (profileImageFile) {
        await updateProfileImage(profileImageFile);
      }

      setSuccess("Client profile created successfully.");
      setTimeout(() => navigate("/explore"), 700);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header">
          <p className="subtitle">Client Profile</p>
          <h1>Create your client profile</h1>
          <p>Add your contact and location so we can recommend artists near you.</p>
        </div>

        <form className="form" onSubmit={handleSubmit}>
          <div className="profile-create-upload">
            <label className="avatar-upload-label">
              <input
                type="file"
                accept="image/*"
                hidden
                onChange={(event) => setProfileImageFile(event.target.files?.[0] || null)}
              />
              <div className="user-avatar user-avatar-xlarge">
                {profileImageFile ? (
                  <img src={URL.createObjectURL(profileImageFile)} alt="Preview" />
                ) : (
                  <span>＋</span>
                )}
              </div>
              <span>Optional profile picture</span>
            </label>
          </div>
          <div className="form-group">
            <label>Phone number</label>
            <input name="phoneNumber" value={form.phoneNumber} onChange={handleChange} />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label>City</label>
              <input name="city" value={form.city} onChange={handleChange} placeholder="Plovdiv" />
            </div>

            <div className="form-group">
              <label>Country</label>
              <input name="country" value={form.country} onChange={handleChange} placeholder="Bulgaria" />
            </div>
          </div>

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}

          <button className="primary-button">Create Client Profile</button>
        </form>
      </section>
    </main>
  );
}

export default CreateClientProfilePage;
