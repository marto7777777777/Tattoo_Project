import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createTattooRequest } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";

function CreateTattooRequestPage() {
  const navigate = useNavigate();
  const params = useParams();
  const storedArtist = JSON.parse(localStorage.getItem("selectedArtist") || "null");
  const [artistId, setArtistId] = useState(params.artistId || storedArtist?.id || "");
  const [form, setForm] = useState({ description: "", placement: "", images: [""] });
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function handleChange(e) { setForm({ ...form, [e.target.name]: e.target.value }); }
  function updateImage(index, value) { const copy = [...form.images]; copy[index] = value; setForm({ ...form, images: copy }); }

  async function handleSubmit(e) {
    e.preventDefault(); setError(""); setSuccess("");
    const requestData = { tattooArtistId: Number(artistId), description: form.description, placement: form.placement, images: form.images.filter((x)=>x.trim()).map((imageUrl)=>({ imageUrl })) };
    try {
      const response = await createTattooRequest(requestData);
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : JSON.stringify(data)); return; }
      setSuccess("Tattoo request created successfully.");
      setTimeout(()=>navigate("/my-requests"), 800);
    } catch { setError("Server connection failed. Please try again."); }
  }

  return (
    <main className="center-container"><section className="request-layout"><aside className="card side-panel"><p className="subtitle">Selected Artist</p><h2>{storedArtist?.studioName || "Artist selected"}</h2><p className="muted">{storedArtist ? `${storedArtist.firstName} ${storedArtist.lastName}` : "Artist details will appear here when opened from Artists page."}</p><div className="info-list"><p><span>Artist ID:</span> {artistId || "Missing"}</p>{storedArtist?.studioAddress && <p><span>Studio:</span> {storedArtist.studioAddress}</p>}{storedArtist?.consultationDurationMinutes && <p><span>Consultation:</span> {storedArtist.consultationDurationMinutes} minutes</p>}</div><p className="muted">The backend needs the tattoo artist ID to create the request.</p></aside>
      <section className="card form-card"><div className="header"><p className="subtitle">Tattoo Request</p><h1>Describe your tattoo idea</h1><p>Add details, placement, and reference image URLs.</p></div><form className="form" onSubmit={handleSubmit}>{!params.artistId && <div className="form-group"><label>Tattoo artist ID</label><input type="number" value={artistId} onChange={(e)=>setArtistId(e.target.value)} /></div>}<div className="form-group"><label>Description</label><textarea name="description" value={form.description} onChange={handleChange} /></div><div className="form-group"><label>Placement</label><input name="placement" value={form.placement} onChange={handleChange} /></div><div className="section"><h2>Reference images</h2>{form.images.map((img,i)=><div className="form-group" key={i}><label>Image URL {i+1}</label><input value={img} onChange={(e)=>updateImage(i,e.target.value)} /></div>)}<button type="button" className="secondary-button" onClick={()=>setForm({...form, images:[...form.images, ""]})}>Add image</button></div>{error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}<button className="primary-button">Send Tattoo Request</button></form></section></section></main>
  );
}
export default CreateTattooRequestPage;
