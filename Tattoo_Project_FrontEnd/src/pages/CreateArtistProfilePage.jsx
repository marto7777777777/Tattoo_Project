import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createArtistProfile } from "../api/artistApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

const emptySchedule = { dayOfWeek: "", startTime: "", endTime: "", scheduleType: "" };

function CreateArtistProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [form, setForm] = useState({
    studioName: "",
    description: "",
    studioAddress: "",
    studioCity: "",
    studioCountry: "",
    studioLatitude: "",
    studioLongitude: "",
    phoneNumber: "",
    consultationDurationMinutes: "",
    offersOnlineConsultation: false,
    requiresDeposit: false,
    depositAmount: "",
    requirements: [""],
    portfolioImages: [""],
    schedules: [emptySchedule],
  });
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
      portfolioImages: form.portfolioImages
        .filter((imageUrl) => imageUrl.trim())
        .map((imageUrl) => ({ imageUrl })),
      schedules: form.schedules
        .filter((schedule) => schedule.dayOfWeek !== "" && schedule.startTime && schedule.endTime && schedule.scheduleType !== "")
        .map((schedule) => ({
          dayOfWeek: Number(schedule.dayOfWeek),
          startTime: `${schedule.startTime}:00`,
          endTime: `${schedule.endTime}:00`,
          scheduleType: Number(schedule.scheduleType),
        })),
    };

    try {
      const response = await createArtistProfile(artistData);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      if (data.token || data.Token) saveAuthToken(data.token || data.Token);

      setSuccess("Tattoo artist profile created successfully.");
      setTimeout(() => navigate("/artist-workspace"), 800);
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
          <p>Add studio information, location, consultation settings, requirements, portfolio, and separate schedules.</p>
        </div>

        <form className="form" onSubmit={handleSubmit}>
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
              <input name="studioAddress" value={form.studioAddress} onChange={handleChange} placeholder="ul. Ivan Vazov 10" />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Studio city</label>
                <input name="studioCity" value={form.studioCity} onChange={handleChange} placeholder="Plovdiv" />
              </div>

              <div className="form-group">
                <label>Studio country</label>
                <input name="studioCountry" value={form.studioCountry} onChange={handleChange} placeholder="Bulgaria" />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Studio latitude optional</label>
                <input name="studioLatitude" type="number" step="any" value={form.studioLatitude} onChange={handleChange} placeholder="42.1354" />
              </div>

              <div className="form-group">
                <label>Studio longitude optional</label>
                <input name="studioLongitude" type="number" step="any" value={form.studioLongitude} onChange={handleChange} placeholder="24.7453" />
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
              <input name="consultationDurationMinutes" type="number" min="15" max="180" value={form.consultationDurationMinutes} onChange={handleChange} />
            </div>

            <div className="checkbox-row">
              <label>
                <input name="offersOnlineConsultation" type="checkbox" checked={form.offersOnlineConsultation} onChange={handleChange} />
                Offers online consultation
              </label>

              <label>
                <input name="requiresDeposit" type="checkbox" checked={form.requiresDeposit} onChange={handleChange} />
                Requires deposit
              </label>
            </div>

            {form.requiresDeposit && (
              <div className="form-group">
                <label>Deposit amount</label>
                <input name="depositAmount" type="number" step="0.01" value={form.depositAmount} onChange={handleChange} />
              </div>
            )}
          </div>

          <div className="section">
            <h2>Requirements</h2>
            {form.requirements.map((requirement, index) => (
              <div className="form-group" key={index}>
                <label>Requirement {index + 1}</label>
                <input value={requirement} onChange={(event) => updateArray("requirements", index, event.target.value)} />
              </div>
            ))}
            <button type="button" className="secondary-button" onClick={() => addArrayItem("requirements")}>Add requirement</button>
          </div>

          <div className="section">
            <h2>Portfolio images</h2>
            {form.portfolioImages.map((imageUrl, index) => (
              <div className="form-group" key={index}>
                <label>Image URL {index + 1}</label>
                <input value={imageUrl} onChange={(event) => updateArray("portfolioImages", index, event.target.value)} />
              </div>
            ))}
            <button type="button" className="secondary-button" onClick={() => addArrayItem("portfolioImages")}>Add portfolio image</button>
          </div>

          <div className="section">
            <h2>Schedule</h2>
            <p className="muted">Add separate blocks for consultations and tattoo sessions.</p>

            {form.schedules.map((schedule, index) => (
              <div className="schedule-row" key={index}>
                <div className="form-group">
                  <label>Day</label>
                  <select value={schedule.dayOfWeek} onChange={(event) => updateSchedule(index, "dayOfWeek", event.target.value)}>
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
                  <input type="time" value={schedule.startTime} onChange={(event) => updateSchedule(index, "startTime", event.target.value)} />
                </div>

                <div className="form-group">
                  <label>End</label>
                  <input type="time" value={schedule.endTime} onChange={(event) => updateSchedule(index, "endTime", event.target.value)} />
                </div>

                <div className="form-group">
                  <label>Type</label>
                  <select value={schedule.scheduleType} onChange={(event) => updateSchedule(index, "scheduleType", event.target.value)}>
                    <option value="">Select type</option>
                    <option value="1">Consultation</option>
                    <option value="0">Tattoo session</option>
                  </select>
                </div>
              </div>
            ))}

            <button type="button" className="secondary-button" onClick={addSchedule}>Add schedule</button>
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
