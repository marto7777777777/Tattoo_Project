import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { completeTattoo } from "../api/tattooSessionApi";
import { readResponse } from "../api/http";

function CompleteTattooPage() {
  const params=useParams(); const navigate=useNavigate();
  const [tattooRequestId,setTattooRequestId]=useState(params.tattooRequestId || "");
  const [error,setError]=useState(""); const [success,setSuccess]=useState("");
  async function handleSubmit(e){e.preventDefault(); setError(""); setSuccess(""); try{const response=await completeTattoo(Number(tattooRequestId)); const data=await readResponse(response); if(!response.ok){setError(typeof data==="string"?data:JSON.stringify(data));return;} setSuccess("Tattoo completed successfully."); setTimeout(()=>navigate("/artist-workspace"),800);}catch{setError("Server connection failed. Please try again.");}}
  return <main className="center-container"><section className="card form-card"><div className="header"><p className="subtitle">Complete Tattoo</p><h1>Mark tattoo as completed</h1><p>This works only when the request has sessions and no remaining planned sessions to book.</p></div><form className="form" onSubmit={handleSubmit}><div className="form-group"><label>Tattoo request ID</label><input type="number" value={tattooRequestId} onChange={(e)=>setTattooRequestId(e.target.value)}/></div>{error&&<p className="error">{error}</p>}{success&&<p className="success">{success}</p>}<button className="primary-button">Complete Tattoo</button></form></section></main>;
}
export default CompleteTattooPage;
