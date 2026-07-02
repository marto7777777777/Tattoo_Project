import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createArtistProfile } from "../api/artistApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

const emptySchedule = { dayOfWeek: "", startTime: "", endTime: "", scheduleType: "" };

function CreateArtistProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [form, setForm] = useState({ studioName: "", description: "", studioAddress: "", phoneNumber: "", consultationDurationMinutes: "", offersOnlineConsultation: false, requiresDeposit: false, depositAmount: "", requirements: [""], portfolioImages: [""], schedules: [emptySchedule] });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(e) { const { name, value, type, checked } = e.target; setForm({ ...form, [name]: type === "checkbox" ? checked : value }); }
  function updateArray(field, index, value) { const copy = [...form[field]]; copy[index] = value; setForm({ ...form, [field]: copy }); }
  function addArrayItem(field) { setForm({ ...form, [field]: [...form[field], ""] }); }
  function updateSchedule(index, field, value) { const copy = [...form.schedules]; copy[index] = { ...copy[index], [field]: value }; setForm({ ...form, schedules: copy }); }
  function addSchedule() { setForm({ ...form, schedules: [...form.schedules, emptySchedule] }); }

  async function handleSubmit(e) {
    e.preventDefault(); setError(""); setSuccess("");
    const artistData = {
      studioName: form.studioName,
      description: form.description,
      studioAddress: form.studioAddress,
      phoneNumber: form.phoneNumber,
      consultationDurationMinutes: Number(form.consultationDurationMinutes),
      offersOnlineConsultation: form.offersOnlineConsultation,
      requiresDeposit: form.requiresDeposit,
      depositAmount: form.requiresDeposit ? Number(form.depositAmount) : null,
      requirements: form.requirements.filter((x) => x.trim()).map((description) => ({ description })),
      portfolioImages: form.portfolioImages.filter((x) => x.trim()).map((imageUrl) => ({ imageUrl })),
      schedules: form.schedules.filter((s) => s.dayOfWeek !== "" && s.startTime && s.endTime && s.scheduleType !== "").map((s) => ({ dayOfWeek: Number(s.dayOfWeek), startTime: `${s.startTime}:00`, endTime: `${s.endTime}:00`, scheduleType: Number(s.scheduleType) })),
    };
    try {
      const response = await createArtistProfile(artistData);
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : JSON.stringify(data)); return; }
      if (data.token || data.Token) saveAuthToken(data.token || data.Token);
      setSuccess("Tattoo artist profile created successfully.");
      setTimeout(() => navigate("/artist-workspace"), 800);
    } catch { setError("Server connection failed. Please try again."); }
  }

  return (
    <main className="center-container"><section className="card form-card form-card-large"><div className="header"><p className="subtitle">Tattoo Artist Profile</p><h1>Create your artist profile</h1><p>Add studio information, consultation settings, requirements, portfolio, and separate schedules.</p></div>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form-group"><label>Studio name</label><input name="studioName" value={form.studioName} onChange={handleChange} /></div>
        <div className="form-group"><label>Description</label><textarea name="description" value={form.description} onChange={handleChange} /></div>
        <div className="form-row"><div className="form-group"><label>Studio address</label><input name="studioAddress" value={form.studioAddress} onChange={handleChange} /></div><div className="form-group"><label>Phone number</label><input name="phoneNumber" value={form.phoneNumber} onChange={handleChange} /></div></div>
        <div className="section"><h2>Consultation settings</h2><div className="form-group"><label>Consultation duration minutes</label><input name="consultationDurationMinutes" type="number" min="15" max="180" value={form.consultationDurationMinutes} onChange={handleChange} /></div><div className="checkbox-row"><label><input name="offersOnlineConsultation" type="checkbox" checked={form.offersOnlineConsultation} onChange={handleChange} /> Offers online consultation</label><label><input name="requiresDeposit" type="checkbox" checked={form.requiresDeposit} onChange={handleChange} /> Requires deposit</label></div>{form.requiresDeposit && <div className="form-group"><label>Deposit amount</label><input name="depositAmount" type="number" step="0.01" value={form.depositAmount} onChange={handleChange} /></div>}</div>
        <div className="section"><h2>Requirements</h2>{form.requirements.map((r,i)=><div className="form-group" key={i}><label>Requirement {i+1}</label><input value={r} onChange={(e)=>updateArray("requirements", i, e.target.value)} /></div>)}<button type="button" className="secondary-button" onClick={()=>addArrayItem("requirements")}>Add requirement</button></div>
        <div className="section"><h2>Portfolio images</h2>{form.portfolioImages.map((img,i)=><div className="form-group" key={i}><label>Image URL {i+1}</label><input value={img} onChange={(e)=>updateArray("portfolioImages", i, e.target.value)} /></div>)}<button type="button" className="secondary-button" onClick={()=>addArrayItem("portfolioImages")}>Add portfolio image</button></div>
        <div className="section"><h2>Schedule</h2><p className="muted">Add separate blocks for consultations and tattoo sessions.</p>{form.schedules.map((s,i)=><div className="schedule-row" key={i}><div className="form-group"><label>Day</label><select value={s.dayOfWeek} onChange={(e)=>updateSchedule(i,"dayOfWeek",e.target.value)}><option value="">Select day</option><option value="1">Monday</option><option value="2">Tuesday</option><option value="3">Wednesday</option><option value="4">Thursday</option><option value="5">Friday</option><option value="6">Saturday</option><option value="0">Sunday</option></select></div><div className="form-group"><label>Start</label><input type="time" value={s.startTime} onChange={(e)=>updateSchedule(i,"startTime",e.target.value)} /></div><div className="form-group"><label>End</label><input type="time" value={s.endTime} onChange={(e)=>updateSchedule(i,"endTime",e.target.value)} /></div><div className="form-group"><label>Type</label><select value={s.scheduleType} onChange={(e)=>updateSchedule(i,"scheduleType",e.target.value)}><option value="">Select type</option><option value="1">Consultation</option><option value="0">Tattoo session</option></select></div></div>)}<button type="button" className="secondary-button" onClick={addSchedule}>Add schedule</button></div>
        {error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}<button className="primary-button">Create Artist Profile</button>
      </form></section></main>
  );
}
export default CreateArtistProfilePage;
