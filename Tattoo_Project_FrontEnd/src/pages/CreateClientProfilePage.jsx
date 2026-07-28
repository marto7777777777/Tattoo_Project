import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { createClientProfile } from "../api/clientApi";
import { updateProfileImage } from "../api/profileApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";
import ImageCropModal from "../components/ImageCropModal";

function CreateClientProfilePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { saveAuthToken } = useAuth();
  const profileRequired = searchParams.get("profileRequired") === "1";
  const returnTo = searchParams.get("returnTo");
  const [form, setForm] = useState({ phoneNumber: "", city: "", country: "" });
  const [profileImageFile, setProfileImageFile] = useState(null);
  const [profileCropFile, setProfileCropFile] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const profilePreview = useMemo(
    () => (profileImageFile ? URL.createObjectURL(profileImageFile) : null),
    [profileImageFile]
  );

  useEffect(() => () => {
    if (profilePreview) URL.revokeObjectURL(profilePreview);
  }, [profilePreview]);

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
      setTimeout(() => navigate(returnTo || "/explore"), 700);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <>
    <main className="center-container client-profile-create-page">
      <section className="card form-card client-profile-create-card">
        <div className="header">
          <p className="subtitle">Client Profile</p>
          <h1>Create your client profile</h1>
          {profileRequired ? (
            <p className="error">
              You need to create a client profile before you can send tattoo requests, add favorites, or manage bookings.
            </p>
          ) : (
            <p>Add your contact and location so we can recommend artists near you.</p>
          )}
        </div>

        <form className="form" onSubmit={handleSubmit}>
          <div className="profile-create-upload">
            <label className="avatar-upload-label">
              <input
                type="file"
                accept="image/*"
                hidden
                onChange={(event) => {
                  const file = event.target.files?.[0] || null;
                  event.target.value = "";
                  if (file) setProfileCropFile(file);
                }}
              />
              <div className="user-avatar user-avatar-xlarge">
                {profilePreview ? (
                  <img src={profilePreview} alt="Preview" />
                ) : (
                  <span>＋</span>
                )}
              </div>
              <span>Optional profile picture</span>
            </label>
          </div>
          <div className="form-group">
            <label>Phone number</label>
            <input name="phoneNumber" type="tel" autoComplete="tel" value={form.phoneNumber} onChange={handleChange} />
          </div>

          <div className="form-row">
            <div className="form-group">
              <label>City</label>
              <input name="city" autoComplete="address-level2" value={form.city} onChange={handleChange} placeholder="Plovdiv" />
            </div>

            <div className="form-group">
              <label>Country</label>
              <input name="country" autoComplete="country-name" value={form.country} onChange={handleChange} placeholder="Bulgaria" />
            </div>
          </div>

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}

          <button className="primary-button">Create Client Profile</button>
        </form>
      </section>
    </main>
    {profileCropFile && (
      <ImageCropModal
        file={profileCropFile}
        title="Adjust profile picture"
        shape="circle"
        aspect={1}
        outputWidth={900}
        onCancel={() => setProfileCropFile(null)}
        onConfirm={(file) => {
          setProfileImageFile(file);
          setProfileCropFile(null);
        }}
      />
    )}
    </>
  );
}

export default CreateClientProfilePage;
