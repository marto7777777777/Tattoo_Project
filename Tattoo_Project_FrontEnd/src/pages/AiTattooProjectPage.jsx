import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { createAiCheckout, downloadAiVersion, editAiProject, generateAiProject, getAiProject } from "../api/aiTattooApi";
import { getImageUrl } from "../utils/images";
import ImageLightbox from "../components/ImageLightbox";

function AiTattooProjectPage(){
 const {projectId}=useParams(); const [params]=useSearchParams(); const navigate=useNavigate();
 const [project,setProject]=useState(null),[selected,setSelected]=useState(null),[instruction,setInstruction]=useState(""),[busy,setBusy]=useState(false),[error,setError]=useState(""),[lightbox,setLightbox]=useState(null);
 const load=()=>getAiProject(projectId).then(p=>{setProject(p);setSelected(p.versions?.at(-1)||null)}).catch(e=>setError(e.message));
 useEffect(()=>{load();},[projectId]);
 useEffect(()=>{if(params.get("payment")==="success"){const timer=setInterval(load,1800);setTimeout(()=>clearInterval(timer),12000);return()=>clearInterval(timer)}},[]);
 const days=useMemo(()=>project?.editingAccessUntil?Math.max(0,Math.ceil((new Date(project.editingAccessUntil)-new Date())/86400000)):null,[project]);
 const edit=async e=>{e.preventDefault();if(!instruction.trim())return;setBusy(true);setError("");try{const p=await editAiProject(project.id,instruction,selected?.id);setProject(p);setSelected(p.versions.at(-1));setInstruction("")}catch(e){setError(e.message)}finally{setBusy(false)}};
 const unlock=async()=>{setBusy(true);try{const c=await createAiCheckout(project.id);window.location.assign(c.url)}catch(e){setError(e.message);setBusy(false)}};
 const generate=async()=>{setBusy(true);try{const p=await generateAiProject(project.id);setProject(p);setSelected(p.versions.at(-1))}catch(e){setError(e.message)}finally{setBusy(false)}};
 const downloadVersion=async()=>{
  if(!selected)return;
  setError("");
  try{
   const blob=await downloadAiVersion(selected.id);
   const objectUrl=URL.createObjectURL(blob);
   const link=document.createElement("a");
   link.href=objectUrl;
   link.download=`inkroute-${project.title.replace(/[^a-z0-9]+/gi,"-").replace(/^-|-$/g,"").toLowerCase()||"tattoo"}-v${selected.versionNumber}.png`;
   document.body.appendChild(link);
   link.click();
   link.remove();
   URL.revokeObjectURL(objectUrl);
  }catch(e){setError(e.message||"The image could not be downloaded.")}
 };
 const useInRequest=()=>{if(!selected)return;localStorage.setItem("aiTattooReference",JSON.stringify({imageUrl:selected.imageUrl,description:project.initialDescription,tattooStyle:project.tattooStyle,placement:project.placement,projectId:project.id}));navigate("/explore")};
 if(!project)return <main className="page-shell"><section className="container"><div className="loading-state">Loading AI project...</div>{error&&<p className="error-message">{error}</p>}</section></main>;
 return <main className="page-shell ai-editor-page"><section className="container">
  <div className="ai-editor-topbar"><div><Link to="/ai-studio" className="back-link">← AI Studio</Link><h1>{project.title}</h1><div className="ai-lock-chips"><span>🔒 {project.tattooStyle}</span><span>🔒 {project.placement}</span></div></div><div className={`ai-access-card ${project.canEdit?"active":"paused"}`}><strong>{project.canEdit?project.editingAccessUntil?"30-day pass active":"Free access":"Project paused"}</strong><small>{project.editingAccessUntil?`${days} days remaining`:`${project.freeEditsRemaining} free improvements remaining`}</small></div></div>
  {error&&<p className="error-message">{error}</p>}
  <div className="ai-editor-layout">
   <aside className="ai-version-panel"><div className="section-heading"><div><p className="subtitle">History</p><h2>Versions</h2></div></div><div className="ai-version-list">{project.versions.map(v=><button className={selected?.id===v.id?"active":""} key={v.id} onClick={()=>setSelected(v)}><img src={getImageUrl(v.imageUrl)} alt=""/><span><strong>Version {v.versionNumber}</strong><small>{new Date(v.createdAt).toLocaleString()}</small></span></button>)}</div></aside>
   <section className="ai-canvas-panel">{selected?<><button className="ai-main-image" onClick={()=>setLightbox(getImageUrl(selected.imageUrl))}><img src={getImageUrl(selected.imageUrl)} alt="Generated tattoo concept"/><span>View full size</span></button><div className="ai-canvas-actions"><button type="button" className="secondary-button" onClick={downloadVersion}>Download version</button><button className="primary-button" onClick={useInRequest}>Use in tattoo request</button></div></>:<div className="ai-awaiting-card"><h2>Project unlocked?</h2><p>Generate the first version after Stripe confirms your 30-day pass.</p><button className="primary-button" disabled={!project.canEdit||busy} onClick={generate}>Generate first version</button></div>}</section>
   <aside className="ai-chat-panel"><div><p className="subtitle">Tattoo refinement</p><h2>Improve this concept</h2><p>Describe one focused change. The AI will keep this as tattoo artwork only — no arm, skin or body mockup. Style and placement remain locked.</p><div className="ai-prompt-tips"><span>Try:</span><button type="button" onClick={()=>setInstruction("Keep the same tattoo, remove any body or skin and show only the isolated tattoo design on a plain background.")}>Isolate the design</button><button type="button" onClick={()=>setInstruction("Keep the same composition, simplify small details and make the tattoo stencil-ready.")}>Simplify details</button><button type="button" onClick={()=>setInstruction("Keep the same subject and style, improve symmetry, line clarity and tattoo readability.")}>Improve readability</button></div></div>{project.canEdit&&selected?<form onSubmit={edit}><textarea rows="7" value={instruction} onChange={e=>setInstruction(e.target.value)} placeholder="Add peonies around the main subject, keep the composition vertical..."/><button className="primary-button" disabled={busy}>{busy?"Creating version...":"Create improvement"}</button></form>:<div className="ai-upgrade-box"><span>🔒</span><h3>Editing is paused</h3><p>Unlock this exact project for 30 days. Your style, placement and history stay unchanged.</p><button className="primary-button" disabled={busy} onClick={unlock}>Unlock 30 days · €4.99</button></div>}</aside>
  </div>
 </section><ImageLightbox imageUrl={lightbox} alt="AI tattoo version" onClose={()=>setLightbox(null)}/></main>
}
export default AiTattooProjectPage;
