import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createConsultation } from "../api/consultationApi";
import { readResponse } from "../api/http";
import { toApiDateTime } from "../utils/format";

function CreateConsultationPage() {
  const params = useParams();
  const navigate = useNavigate();
  const [form, setForm] = useState({ tattooRequestId: params.tattooRequestId || "", startTime: "", notes: "" });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  function handleChange(e){ setForm({...form,[e.target.name]:e.target.value}); }
  async function handleSubmit(e){ e.preventDefault(); setError(""); setSuccess(""); try{ const response=await createConsultation({ tattooRequestId:Number(form.tattooRequestId), startTime:toApiDateTime(form.startTime), notes:form.notes }); const data=await readResponse(response); if(!response.ok){setError(typeof data==="string"?data:JSON.stringify(data));return;} setSuccess("Consultation booked successfully."); setTimeout(()=>navigate("/bookings"),800);}catch{setError("Server connection failed. Please try again.");} }
  return <main className="center-container"><section className="card form-card"><div className="header"><p className="subtitle">Consultation</p><h1>Book consultation</h1><p>Choose only the start time. Backend calculates the end time from the artist consultation duration.</p></div><form className="form" onSubmit={handleSubmit}><div className="form-group"><label>Tattoo request ID</label><input name="tattooRequestId" type="number" value={form.tattooRequestId} onChange={handleChange}/></div><div className="form-group"><label>Start time</label><input name="startTime" type="datetime-local" value={form.startTime} onChange={handleChange}/></div><div className="form-group"><label>Notes</label><textarea name="notes" value={form.notes} onChange={handleChange}/></div>{error&&<p className="error">{error}</p>}{success&&<p className="success">{success}</p>}<button className="primary-button">Book Consultation</button></form></section></main>;
}
export default CreateConsultationPage;
