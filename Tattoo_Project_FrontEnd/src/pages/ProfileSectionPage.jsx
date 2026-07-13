import { useEffect, useMemo, useState } from "react";
import { Link, Navigate, NavLink, useParams } from "react-router-dom";
import {
  addPortfolioImage,
  addRequirement,
  changePasswordWithCode,
  deletePortfolioImage,
  deleteRequirement,
  getMyProfile,
  sendPasswordChangeCode,
  updateBoolField,
  updateNumberField,
  updateProfileImage,
  updateRequirement,
  updateStringField,
} from "../api/profileApi";
import UserAvatar from "../components/UserAvatar";
import { useAuth } from "../context/AuthContext";
import { getImageUrl } from "../utils/images";

const sectionTitles = {
  user: "Account & identity",
  contact: "Contact information",
  studio: "Studio profile",
  consultation: "Consultation settings",
  deposit: "Deposit settings",
  portfolio: "Portfolio manager",
};

const sectionDescriptions = {
  user: "Manage your name, email, profile photo and account password.",
  contact: "Keep the phone number and location clients use to reach you up to date.",
  studio: "Edit how your studio appears to clients, including requirements and address.",
  consultation: "Control consultation duration and whether online consultations are available.",
  deposit: "Choose whether projects require a deposit and set the amount.",
  portfolio: "Upload and manage the work clients see on your artist profile.",
};

const sectionIcons = {
  user: "◉",
  contact: "✦",
  studio: "⌂",
  consultation: "◷",
  deposit: "◇",
  portfolio: "▦",
};

function ProfileSectionPage() {
  const { section = "user" } = useParams();
  const { isArtist } = useAuth();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [editingKey, setEditingKey] = useState(null);
  const [editValue, setEditValue] = useState("");
  const [newRequirement, setNewRequirement] = useState("");
  const [passwordStep, setPasswordStep] = useState("idle");
  const [passwordForm, setPasswordForm] = useState({ code: "", newPassword: "", confirmNewPassword: "" });

  const allowedSections = useMemo(() => {
    const sections = ["user", "contact"];
    if (isArtist) sections.push("studio", "consultation", "deposit", "portfolio");
    return sections;
  }, [isArtist]);

  useEffect(() => {
    loadProfile();
  }, []);

  async function loadProfile() {
    setLoading(true);
    setError("");

    try {
      const data = await getMyProfile();
      setProfile(data);
    } catch (err) {
      setError(err.message || "Profile could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  function startEdit(field) {
    setEditingKey(field.key);
    setEditValue(field.value ?? "");
    setError("");
    setSuccess("");
  }

  async function saveField(field) {
    setError("");
    setSuccess("");

    try {
      if (field.type === "bool") {
        await updateBoolField(field.path, editValue === true || editValue === "true");
      } else if (field.type === "number") {
        await updateNumberField(field.path, editValue);
      } else {
        await updateStringField(field.path, editValue);
      }

      setEditingKey(null);
      setEditValue("");
      setSuccess("Updated successfully.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Update failed.");
    }
  }

  async function handleProfileImageChange(event) {
    const file = event.target.files?.[0];
    if (!file) return;

    setError("");
    setSuccess("");

    try {
      await updateProfileImage(file);
      setSuccess("Profile picture updated successfully.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Image upload failed.");
    }
  }

  async function handleSendPasswordCode() {
    setError("");
    setSuccess("");

    try {
      await sendPasswordChangeCode();
      setPasswordStep("code");
      setSuccess("Password change code sent to your email. It expires in 10 minutes. If you do not see it, check your Spam folder.");
    } catch (err) {
      setError(err.message || "Password change code could not be sent.");
    }
  }

  async function handleChangePassword() {
    setError("");
    setSuccess("");

    try {
      await changePasswordWithCode(
        passwordForm.code,
        passwordForm.newPassword,
        passwordForm.confirmNewPassword
      );

      setPasswordStep("success");
      setPasswordForm({ code: "", newPassword: "", confirmNewPassword: "" });
      setSuccess("Password changed successfully.");
      setTimeout(() => setPasswordStep("idle"), 3000);
    } catch (err) {
      setError(err.message || "Password could not be changed.");
    }
  }

  async function handleAddRequirement() {
    if (!newRequirement.trim()) return;

    try {
      await addRequirement(newRequirement);
      setNewRequirement("");
      setSuccess("Requirement added.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Requirement could not be added.");
    }
  }

  async function handleRequirementUpdate(requirement) {
    const value = window.prompt("Edit requirement", requirement.description);
    if (value === null) return;

    try {
      await updateRequirement(requirement.id, value);
      setSuccess("Requirement updated.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Requirement could not be updated.");
    }
  }

  async function handleDeleteRequirement(id) {
    try {
      await deleteRequirement(id);
      setSuccess("Requirement deleted.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Requirement could not be deleted.");
    }
  }

  async function handlePortfolioUpload(event) {
    const files = Array.from(event.target.files || []);
    if (!files.length) return;

    try {
      for (const file of files) {
        await addPortfolioImage(file);
      }
      setSuccess("Portfolio image uploaded.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Portfolio upload failed.");
    }
  }

  async function handleDeletePortfolioImage(id) {
    try {
      await deletePortfolioImage(id);
      setSuccess("Portfolio image deleted.");
      await loadProfile();
    } catch (err) {
      setError(err.message || "Portfolio image could not be deleted.");
    }
  }

  if (!allowedSections.includes(section)) {
    return <Navigate to="/profile/user" replace />;
  }

  if (loading) {
    return <main className="page-shell"><div className="page-container"><p className="muted">Loading profile...</p></div></main>;
  }

  if (!profile) {
    return <main className="page-shell"><div className="page-container"><p className="error">{error || "Profile could not be loaded."}</p></div></main>;
  }

  const fields = getFieldsForSection(section, profile);

  return (
    <main className="page-shell profile-page-shell">
      <div className="page-container profile-page">
      <section className="profile-settings-hero card">
        <div className="profile-identity-block">
          <label className="profile-main-avatar-upload" title="Change profile picture">
            <input
              type="file"
              accept="image/*"
              hidden
              onChange={handleProfileImageChange}
            />

            <div className="profile-main-avatar-wrapper">
              <UserAvatar
                firstName={profile.firstName}
                lastName={profile.lastName}
                email={profile.email}
                imageUrl={profile.profileImageUrl}
                size="xlarge"
              />
              <div className="profile-avatar-edit-overlay">Change photo</div>
            </div>
          </label>

          <div className="profile-identity-copy">
            <p className="subtitle">Profile settings</p>
            <h1>{profile.firstName} {profile.lastName}</h1>
            <p>{profile.email}</p>
            <div className="profile-role-pills">
              <span>Client profile</span>
              {isArtist && <span>Artist profile</span>}
            </div>
          </div>
        </div>

        <div className="profile-settings-summary">
          <div><strong>{allowedSections.length}</strong><span>Settings sections</span></div>
          <div><strong>{profile.artist?.portfolioImages?.length || 0}</strong><span>Portfolio images</span></div>
          <div><strong>{profile.artist?.requirements?.length || 0}</strong><span>Studio requirements</span></div>
        </div>
      </section>

      <div className="profile-layout profile-settings-layout">
        <aside className="card profile-side-nav profile-settings-nav">
          <div className="settings-nav-heading">
            <span>Settings</span>
            <small>Edit each area separately</small>
          </div>
          {allowedSections.map((item) => (
            <NavLink key={item} to={`/profile/${item}`}>
              <span className="settings-nav-icon">{sectionIcons[item]}</span>
              <span><strong>{sectionTitles[item]}</strong><small>{sectionDescriptions[item]}</small></span>
            </NavLink>
          ))}
          {isArtist && (
            <Link className="profile-schedule-link" to="/my-studio/calendar">
              <span className="settings-nav-icon">▤</span>
              <span><strong>Working schedule</strong><small>Edit working days, hours and time off.</small></span>
            </Link>
          )}
        </aside>

        <section className="card profile-section-card profile-settings-content">
          <div className="profile-section-heading">
            <span className="profile-section-icon">{sectionIcons[section]}</span>
            <div>
              <p className="subtitle">Settings section</p>
              <h2>{sectionTitles[section]}</h2>
              <p>{sectionDescriptions[section]}</p>
            </div>
          </div>

          {section !== "portfolio" && (
            <div className="profile-field-list">
              {fields.map((field) => (
                <div className="profile-field-row" key={field.key}>
                  <div>
                    <span className="field-label">{field.label}</span>
                    {editingKey === field.key ? (
                      field.type === "bool" ? (
                        <select value={String(editValue)} onChange={(event) => setEditValue(event.target.value)}>
                          <option value="true">Yes</option>
                          <option value="false">No</option>
                        </select>
                      ) : (
                        <input
                          type={field.type === "number" ? "number" : "text"}
                          value={editValue ?? ""}
                          onChange={(event) => setEditValue(event.target.value)}
                        />
                      )
                    ) : (
                      <strong>{formatFieldValue(field)}</strong>
                    )}
                  </div>

                  {editingKey === field.key ? (
                    <div className="inline-actions">
                      <button className="primary-button compact-button" type="button" onClick={() => saveField(field)}>Save</button>
                      <button className="secondary-button compact-button" type="button" onClick={() => setEditingKey(null)}>Cancel</button>
                    </div>
                  ) : (
                    <button className="secondary-button compact-button" type="button" onClick={() => startEdit(field)}>Edit</button>
                  )}
                </div>
              ))}

              {section === "user" && (
                <div className="profile-field-row">
                  <div>
                    <span className="field-label">Password</span>
                    {passwordStep === "code" ? (
                      <div className="inline-form-row">
                        <input
                          value={passwordForm.code}
                          onChange={(event) => setPasswordForm({ ...passwordForm, code: event.target.value.replace(/\D/g, "").slice(0, 6) })}
                          inputMode="numeric"
                          maxLength="6"
                          placeholder="Code"
                        />
                        <input
                          type="password"
                          value={passwordForm.newPassword}
                          onChange={(event) => setPasswordForm({ ...passwordForm, newPassword: event.target.value })}
                          placeholder="New password"
                        />
                        <input
                          type="password"
                          value={passwordForm.confirmNewPassword}
                          onChange={(event) => setPasswordForm({ ...passwordForm, confirmNewPassword: event.target.value })}
                          placeholder="Confirm password"
                        />
                      </div>
                    ) : (
                      <strong>••••••••</strong>
                    )}
                  </div>

                  {passwordStep === "code" ? (
                    <div className="inline-actions">
                      <button className="primary-button compact-button" type="button" onClick={handleChangePassword}>Save</button>
                      <button className="secondary-button compact-button" type="button" onClick={() => setPasswordStep("idle")}>Cancel</button>
                    </div>
                  ) : (
                    <button className="secondary-button compact-button" type="button" onClick={handleSendPasswordCode}>Change</button>
                  )}
                </div>
              )}
            </div>
          )}

          {section === "studio" && (
            <div className="section profile-extra-section">
              <h3>Requirements</h3>
              <div className="inline-form-row">
                <input value={newRequirement} onChange={(event) => setNewRequirement(event.target.value)} placeholder="Add new requirement" />
                <button className="primary-button compact-button" type="button" onClick={handleAddRequirement}>Add</button>
              </div>

              <div className="small-list">
                {(profile.artist?.requirements || []).map((requirement) => (
                  <div className="small-list-row" key={requirement.id}>
                    <span>{requirement.description}</span>
                    <div className="inline-actions">
                      <button className="secondary-button compact-button" type="button" onClick={() => handleRequirementUpdate(requirement)}>Edit</button>
                      <button className="danger-button compact-button" type="button" onClick={() => handleDeleteRequirement(requirement.id)}>Delete</button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {section === "portfolio" && (
            <div className="section">
              <div className="portfolio-manage-grid">
                <label className="portfolio-upload-card" title="Add portfolio images">
                  <input type="file" accept="image/*" multiple hidden onChange={handlePortfolioUpload} />
                  <span className="portfolio-upload-card-icon" aria-hidden="true">＋</span>
                  <strong>Add image</strong>
                  <small>JPG, PNG or WEBP</small>
                </label>

                {(profile.artist?.portfolioImages || []).map((image) => (
                  <div className="portfolio-manage-card" key={image.id}>
                    <img src={getImageUrl(image.imageUrl)} alt="Portfolio" />
                    <button className="portfolio-delete-button" type="button" onClick={() => handleDeletePortfolioImage(image.id)}>
                      Delete
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}
        </section>
      </div>
      </div>
    </main>
  );
}

function getFieldsForSection(section, profile) {
  const artist = profile.artist || {};

  if (section === "user") {
    return [
      { key: "firstName", label: "First name", value: profile.firstName, path: "/api/Profile/user/first-name" },
      { key: "lastName", label: "Last name", value: profile.lastName, path: "/api/Profile/user/last-name" },
      { key: "email", label: "Email", value: profile.email, path: "/api/Profile/user/email" },
    ];
  }

  if (section === "contact") {
    return [
      { key: "phoneNumber", label: "Phone number", value: profile.phoneNumber, path: "/api/Profile/contact/phone-number" },
      { key: "city", label: "City", value: profile.city, path: "/api/Profile/contact/city" },
      { key: "country", label: "Country", value: profile.country, path: "/api/Profile/contact/country" },
    ];
  }

  if (section === "studio") {
    return [
      { key: "studioName", label: "Studio name", value: artist.studioName, path: "/api/Profile/studio/studio-name" },
      { key: "description", label: "Description", value: artist.description, path: "/api/Profile/studio/description" },
      { key: "studioCountry", label: "Studio country", value: artist.studioCountry, path: "/api/Profile/studio/studio-country" },
      { key: "studioCity", label: "Studio city", value: artist.studioCity, path: "/api/Profile/studio/studio-city" },
      { key: "studioAddress", label: "Studio address", value: artist.studioAddress, path: "/api/Profile/studio/studio-address" },
    ];
  }

  if (section === "consultation") {
    return [
      { key: "consultationDurationMinutes", label: "Consultation duration minutes", value: artist.consultationDurationMinutes, type: "number", path: "/api/Profile/consultation/duration" },
      { key: "offersOnlineConsultation", label: "Offers online consultation", value: artist.offersOnlineConsultation, type: "bool", path: "/api/Profile/consultation/offers-online" },
    ];
  }

  if (section === "deposit") {
    return [
      { key: "requiresDeposit", label: "Requires deposit", value: artist.requiresDeposit, type: "bool", path: "/api/Profile/deposit/requires-deposit" },
      { key: "depositAmount", label: "Deposit amount", value: artist.depositAmount ?? "", type: "number", path: "/api/Profile/deposit/amount" },
    ];
  }

  return [];
}

function formatFieldValue(field) {
  if (field.type === "bool") return field.value ? "Yes" : "No";
  if (field.value === null || field.value === undefined || field.value === "") return "Not added yet";
  return String(field.value);
}

export default ProfileSectionPage;
