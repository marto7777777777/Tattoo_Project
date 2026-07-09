import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createArtistProfile } from "../api/artistApi";
import { addPortfolioImage, updateProfileImage } from "../api/profileApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

const emptySchedule = {
  dayOfWeek: "",
  startTime: "",
  endTime: "",
  scheduleType: "",
};

function CreateArtistProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();

  const [form, setForm] = useState({
    studioName: "",
    description: "",
    studioAddress: "",
    studioCity: "",
    studioCountry: "",
    studioLatitude: null,
    studioLongitude: null,
    phoneNumber: "",
    consultationDurationMinutes: "",
    offersOnlineConsultation: false,
    requiresDeposit: false,
    depositAmount: "",
    requirements: [""],
    portfolioImages: [],
    schedules: [emptySchedule],
  });

  const [profileImageFile, setProfileImageFile] = useState(null);
  const [portfolioImageFiles, setPortfolioImageFiles] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(event) {
    const { name, value, type, checked } = event.target;
    setForm({ ...form, [name]: type === "checkbox" ? checked : value });
  }

  function updateArray(field, index, value) {
    const copy = [...form[field]];
    copy[index] = value;
    setForm({ ...form, [field]: copy });
  }

  function addArrayItem(field) {
    setForm({ ...form, [field]: [...form[field], ""] });
  }

  function updateSchedule(index, field, value) {
    const copy = [...form.schedules];
    copy[index] = { ...copy[index], [field]: value };
    setForm({ ...form, schedules: copy });
  }

  function addSchedule() {
    setForm({ ...form, schedules: [...form.schedules, emptySchedule] });
  }

  function toNullableNumber(value) {
    if (value === "" || value === null || value === undefined) return null;
    return Number(value);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (!form.studioName.trim()) {
      setError("Studio name is required.");
      return;
    }

    if (!form.description.trim()) {
      setError("Description is required.");
      return;
    }

    if (!form.studioAddress.trim() || !form.studioCity.trim() || !form.studioCountry.trim()) {
      setError("Please enter studio address, city and country.");
      return;
    }

    if (!form.phoneNumber.trim()) {
      setError("Phone number is required.");
      return;
    }

    if (
      !form.consultationDurationMinutes ||
      Number(form.consultationDurationMinutes) < 15 ||
      Number(form.consultationDurationMinutes) > 180
    ) {
      setError("Consultation duration must be between 15 and 180 minutes.");
      return;
    }

    if (form.requiresDeposit && Number(form.depositAmount) <= 0) {
      setError("Deposit amount must be greater than 0.");
      return;
    }

    const schedules = form.schedules
      .filter(
        (schedule) =>
          schedule.dayOfWeek !== "" &&
          schedule.startTime &&
          schedule.endTime &&
          schedule.scheduleType !== ""
      )
      .map((schedule) => ({
        dayOfWeek: Number(schedule.dayOfWeek),
        startTime: `${schedule.startTime}:00`,
        endTime: `${schedule.endTime}:00`,
        scheduleType: Number(schedule.scheduleType),
      }));

    const hasConsultationSchedule = schedules.some((schedule) => schedule.scheduleType === 1);
    const hasTattooSessionSchedule = schedules.some((schedule) => schedule.scheduleType === 0);

    if (!hasConsultationSchedule || !hasTattooSessionSchedule) {
      setError("Please add at least one consultation schedule and one tattoo session schedule.");
      return;
    }

    const artistData = {
      studioName: form.studioName,
      description: form.description,
      studioAddress: form.studioAddress,
      studioCity: form.studioCity,
      studioCountry: form.studioCountry,
      studioLatitude: toNullableNumber(form.studioLatitude),
      studioLongitude: toNullableNumber(form.studioLongitude),
      phoneNumber: form.phoneNumber,
      consultationDurationMinutes: Number(form.consultationDurationMinutes),
      offersOnlineConsultation: form.offersOnlineConsultation,
      requiresDeposit: form.requiresDeposit,
      depositAmount: form.requiresDeposit ? Number(form.depositAmount) : null,
      requirements: form.requirements
        .filter((requirement) => requirement.trim())
        .map((description) => ({ description })),
      portfolioImages: [],
      schedules,
    };

    try {
      const response = await createArtistProfile(artistData);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      if (data.token || data.Token) saveAuthToken(data.token || data.Token);

      if (profileImageFile) {
        await updateProfileImage(profileImageFile);
      }

      for (const file of portfolioImageFiles) {
        await addPortfolioImage(file);
      }

      setSuccess("Tattoo artist profile created successfully.");
      setTimeout(() => navigate("/my-studio"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="center-container">
      <section className="card form-card form-card-large">
        <div className="header">
          <p className="subtitle">Tattoo Artist Profile</p>
          <h1>Create your artist profile</h1>
          <p>
            Add studio information, location, consultation settings,
            requirements, portfolio, and separate schedules.
          </p>
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
            <label>Studio name</label>
            <input name="studioName" value={form.studioName} onChange={handleChange} />
          </div>

          <div className="form-group">
            <label>Description</label>
            <textarea name="description" value={form.description} onChange={handleChange} />
          </div>

          <div className="section">
            <h2>Studio location</h2>

            <div className="form-group">
              <label>Studio address</label>
              <input
                name="studioAddress"
                value={form.studioAddress}
                onChange={handleChange}
                placeholder="ul. Ivan Vazov 10"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Studio city</label>
                <input
                  name="studioCity"
                  value={form.studioCity}
                  onChange={handleChange}
                  placeholder="Plovdiv"
                />
              </div>

              <div className="form-group">
                <label>Studio country</label>
                <input
                  name="studioCountry"
                  value={form.studioCountry}
                  onChange={handleChange}
                  placeholder="Bulgaria"
                />
              </div>
            </div>
          </div>

          <div className="form-group">
            <label>Phone number</label>
            <input name="phoneNumber" value={form.phoneNumber} onChange={handleChange} />
          </div>

          <div className="section">
            <h2>Consultation settings</h2>

            <div className="form-group">
              <label>Consultation duration minutes</label>
              <input
                name="consultationDurationMinutes"
                type="number"
                min="15"
                max="180"
                value={form.consultationDurationMinutes}
                onChange={handleChange}
              />
            </div>

            <div className="checkbox-row">
              <label>
                <input
                  name="offersOnlineConsultation"
                  type="checkbox"
                  checked={form.offersOnlineConsultation}
                  onChange={handleChange}
                />
                Offers online consultation
              </label>

              <label>
                <input
                  name="requiresDeposit"
                  type="checkbox"
                  checked={form.requiresDeposit}
                  onChange={handleChange}
                />
                Requires deposit
              </label>
            </div>

            {form.requiresDeposit && (
              <div className="form-group">
                <label>Deposit amount</label>
                <input
                  name="depositAmount"
                  type="number"
                  step="0.01"
                  value={form.depositAmount}
                  onChange={handleChange}
                />
              </div>
            )}
          </div>

          <div className="section">
            <h2>Requirements</h2>
            {form.requirements.map((requirement, index) => (
              <div className="form-group" key={index}>
                <label>Requirement {index + 1}</label>
                <input
                  value={requirement}
                  onChange={(event) => updateArray("requirements", index, event.target.value)}
                />
              </div>
            ))}
            <button type="button" className="secondary-button" onClick={() => addArrayItem("requirements")}>
              Add requirement
            </button>
          </div>

          <div className="section">
            <h2>Portfolio images</h2>
            <label className="portfolio-upload-tile">
              <input
                type="file"
                accept="image/*"
                multiple
                hidden
                onChange={(event) => setPortfolioImageFiles(Array.from(event.target.files || []))}
              />
              <span>＋</span>
              <strong>Select portfolio photos</strong>
              <small>They will be uploaded after your artist profile is created.</small>
            </label>

            {portfolioImageFiles.length > 0 && (
              <div className="portfolio-preview-grid">
                {portfolioImageFiles.map((file, index) => (
                  <img key={`${file.name}-${index}`} src={URL.createObjectURL(file)} alt="Portfolio preview" />
                ))}
              </div>
            )}
          </div>

          <div className="section">
            <h2>Schedule</h2>
            <p className="muted">Add separate blocks for consultations and tattoo sessions.</p>

            {form.schedules.map((schedule, index) => (
              <div className="schedule-row" key={index}>
                <div className="form-group">
                  <label>Day</label>
                  <select
                    value={schedule.dayOfWeek}
                    onChange={(event) => updateSchedule(index, "dayOfWeek", event.target.value)}
                  >
                    <option value="">Select day</option>
                    <option value="1">Monday</option>
                    <option value="2">Tuesday</option>
                    <option value="3">Wednesday</option>
                    <option value="4">Thursday</option>
                    <option value="5">Friday</option>
                    <option value="6">Saturday</option>
                    <option value="0">Sunday</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Start</label>
                  <input
                    type="time"
                    value={schedule.startTime}
                    onChange={(event) => updateSchedule(index, "startTime", event.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label>End</label>
                  <input
                    type="time"
                    value={schedule.endTime}
                    onChange={(event) => updateSchedule(index, "endTime", event.target.value)}
                  />
                </div>

                <div className="form-group">
                  <label>Type</label>
                  <select
                    value={schedule.scheduleType}
                    onChange={(event) => updateSchedule(index, "scheduleType", event.target.value)}
                  >
                    <option value="">Select type</option>
                    <option value="1">Consultation</option>
                    <option value="0">Tattoo session</option>
                  </select>
                </div>
              </div>
            ))}

            <button type="button" className="secondary-button" onClick={addSchedule}>
              Add schedule
            </button>
          </div>

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}

          <button className="primary-button">Create Artist Profile</button>
        </form>
      </section>
    </main>
  );
}

export default CreateArtistProfilePage;
