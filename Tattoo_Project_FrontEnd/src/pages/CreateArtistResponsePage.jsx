import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createArtistResponse, rejectTattooRequest } from "../api/artistResponseApi";
import { readResponse } from "../api/http";

function CreateArtistResponsePage() {
  const params = useParams(); const navigate=useNavigate();
  const [form,setForm]=useState({ tattooRequestId: params.tattooRequestId || "", estimatedPrice:"", estimatedHours:"", responseMessage:"" });
  const [error,setError]=useState(""); const [success,setSuccess]=useState("");
  function handleChange(e){setForm({...form,[e.target.name]:e.target.value});}
  async function handleSubmit(e){e.preventDefault(); setError(""); setSuccess(""); try{const response=await createArtistResponse({ tattooRequestId:Number(form.tattooRequestId), estimatedPrice:Number(form.estimatedPrice), estimatedHours:Number(form.estimatedHours), responseMessage:form.responseMessage }); const data=await readResponse(response); if(!response.ok){setError(typeof data==="string"?data:JSON.stringify(data));return;} setSuccess("Artist response created successfully."); setTimeout(()=>navigate("/artist-workspace"),800);}catch{setError("Server connection failed. Please try again.");}}
  async function handleReject(){setError(""); setSuccess(""); try{const response=await rejectTattooRequest(Number(form.tattooRequestId)); const data=await readResponse(response); if(!response.ok){setError(typeof data==="string"?data:JSON.stringify(data));return;} setSuccess("Tattoo request rejected successfully.");}catch{setError("Server connection failed. Please try again.");}}
  return <main className="center-container"><section className="card form-card"><div className="header"><p className="subtitle">Artist Response</p><h1>Respond to tattoo request</h1><p>Create an acceptance response or reject the request.</p></div><form className="form" onSubmit={handleSubmit}><div className="form-group"><label>Tattoo request ID</label><input name="tattooRequestId" type="number" value={form.tattooRequestId} onChange={handleChange}/></div><div className="form-row"><div className="form-group"><label>Estimated price</label><input name="estimatedPrice" type="number" step="0.01" value={form.estimatedPrice} onChange={handleChange}/></div><div className="form-group"><label>Estimated hours</label><input name="estimatedHours" type="number" value={form.estimatedHours} onChange={handleChange}/></div></div><div className="form-group"><label>Response message</label><textarea name="responseMessage" value={form.responseMessage} onChange={handleChange}/></div>{error&&<p className="error">{error}</p>}{success&&<p className="success">{success}</p>}<div className="action-row"><button className="primary-button">Create Response</button><button type="button" className="danger-button" onClick={handleReject}>Reject Request</button></div></form></section></main>;
}
export default CreateArtistResponsePage;
