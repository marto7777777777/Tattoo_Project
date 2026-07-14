import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAiProjects } from "../api/aiTattooApi";
import { getImageUrl } from "../utils/images";

function AiStudioPage(){
 const [projects,setProjects]=useState([]),[error,setError]=useState("");
 useEffect(()=>{getAiProjects().then(setProjects).catch(e=>setError(e.message));},[]);
 const freeUsed=projects.some(p=>p.isFreeProject);
 return <main className="page-shell ai-studio-page"><section className="container">
  <div className="ai-studio-hero"><div><p className="subtitle">InkRoute AI Studio</p><h1>Shape the tattoo before the first session.</h1><p>Create a tattoo-only concept, refine it version by version, then send it directly into the InkRoute workflow.</p></div><Link className="primary-button" to={`/ai-studio/new${freeUsed?"?paid=1":""}`}>{freeUsed?"Start paid project":"Create free project"}</Link></div>
  <div className="ai-benefit-strip"><span><strong>1 free project</strong><small>Initial generation included</small></span><span><strong>2 free improvements</strong><small>Download every version</small></span><span><strong>Locked direction</strong><small>Style and placement stay fixed</small></span><span><strong>30-day unlock</strong><small>Renew only when needed</small></span></div>
  {error&&<p className="error-message">{error}</p>}
  <div className="section-heading"><div><p className="subtitle">Your workspace</p><h2>AI tattoo projects</h2></div></div>
  <div className="ai-project-grid">{projects.map(p=>{const last=p.versions?.at(-1);return <Link className="ai-project-card" to={`/ai-studio/${p.id}`} key={p.id}><div className="ai-project-cover">{last?<img src={getImageUrl(last.imageUrl)} alt=""/>:<span>Awaiting unlock</span>}<em className={p.canEdit?"active":"paused"}>{p.canEdit?"Active":"Paused"}</em></div><div><h3>{p.title}</h3><p>{p.tattooStyle} · {p.placement}</p><small>{p.versions?.length||0} versions</small></div></Link>})}{projects.length===0&&<div className="empty-state"><h3>Your first concept starts here.</h3><p>Create one free project with two AI improvements.</p></div>}</div>
 </section></main>
}
export default AiStudioPage;
