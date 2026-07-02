import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { createClientProfile } from "../api/clientApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

function CreateClientProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [phoneNumber, setPhoneNumber] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function handleSubmit(event) {
    event.preventDefault(); setError(""); setSuccess("");
    try {
      const response = await createClientProfile({ phoneNumber });
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : JSON.stringify(data)); return; }
      if (data.token || data.Token) saveAuthToken(data.token || data.Token);
      setSuccess("Client profile created successfully.");
      setTimeout(() => navigate("/artists"), 700);
    } catch { setError("Server connection failed. Please try again."); }
  }

  return <main className="center-container"><section className="card form-card"><div className="header"><p className="subtitle">Client Profile</p><h1>Create your client profile</h1><p>Add your phone number so artists can contact you.</p></div><form className="form" onSubmit={handleSubmit}><div className="form-group"><label>Phone number</label><input value={phoneNumber} onChange={(e)=>setPhoneNumber(e.target.value)} /></div>{error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}<button className="primary-button">Create Client Profile</button></form></section></main>;
}
export default CreateClientProfilePage;
