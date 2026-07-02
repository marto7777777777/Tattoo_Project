import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { completeConsultation, rejectConsultation } from "../api/consultationApi";
import { readResponse } from "../api/http";

function CompleteConsultationPage() {
  const params=useParams(); const navigate=useNavigate();
  const [tattooRequestId,setTattooRequestId]=useState(params.tattooRequestId || "");
  const [sessions,setSessions]=useState([{ price:"", durationHours:"" }]);
  const [error,setError]=useState(""); const [success,setSuccess]=useState("");
  function updateSession(index,field,value){const copy=[...sessions]; copy[index]={...copy[index],[field]:value}; setSessions(copy);}
  async function handleSubmit(e){e.preventDefault(); setError(""); setSuccess(""); const data={sessionsToBook:sessions.length, priceForSession:sessions.map(s=>Number(s.price)), durationHoursForSession:sessions.map(s=>Number(s.durationHours))}; try{const response=await completeConsultation(Number(tattooRequestId),data); const result=await readResponse(response); if(!response.ok){setError(typeof result==="string"?result:JSON.stringify(result));return;} setSuccess("Consultation completed successfully."); setTimeout(()=>navigate("/artist-workspace"),800);}catch{setError("Server connection failed. Please try again.");}}
  async function handleReject(){setError(""); setSuccess(""); try{const response=await rejectConsultation(Number(tattooRequestId)); const data=await readResponse(response); if(!response.ok){setError(typeof data==="string"?data:JSON.stringify(data));return;} setSuccess("Consultation rejected successfully.");}catch{setError("Server connection failed. Please try again.");}}
  return <main className="center-container"><section className="card form-card"><div className="header"><p className="subtitle">Complete Consultation</p><h1>Define planned sessions</h1><p>Set price and duration for every tattoo session the client can book.</p></div><form className="form" onSubmit={handleSubmit}><div className="form-group"><label>Tattoo request ID</label><input type="number" value={tattooRequestId} onChange={(e)=>setTattooRequestId(e.target.value)}/></div><div className="section"><h2>Sessions</h2>{sessions.map((s,i)=><div className="form-row" key={i}><div className="form-group"><label>Session {i+1} price</label><input type="number" step="0.01" value={s.price} onChange={(e)=>updateSession(i,"price",e.target.value)}/></div><div className="form-group"><label>Duration hours</label><input type="number" value={s.durationHours} onChange={(e)=>updateSession(i,"durationHours",e.target.value)}/></div></div>)}<button type="button" className="secondary-button" onClick={()=>setSessions([...sessions,{price:"",durationHours:""}])}>Add session</button></div>{error&&<p className="error">{error}</p>}{success&&<p className="success">{success}</p>}<div className="action-row"><button className="primary-button">Complete Consultation</button><button type="button" className="danger-button" onClick={handleReject}>Reject Consultation</button></div></form></section></main>;
}
export default CompleteConsultationPage;
