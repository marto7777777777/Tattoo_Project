import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createFreeAiProject } from "../api/aiTattooApi";
import { PLACEMENTS, TATTOO_STYLES } from "../data/tattooOptions";

function Selector({title,items,value,onChange,trackRef}){const move=d=>{const el=trackRef.current;if(!el)return;const card=el.querySelector(".visual-carousel-card");if(card)el.scrollBy({left:d*(card.getBoundingClientRect().width+12)*3,behavior:"smooth"});};return <section className="section carousel-section"><div className="selection-section-head"><div><p className="subtitle">Locked after generation</p><h2>{title}</h2></div><span className="selection-value">{value||"Choose one"}</span></div><div className="selector-carousel-shell"><button className="carousel-arrow carousel-arrow-left" onClick={()=>move(-1)} type="button">‹</button><div className="selector-carousel-track" ref={trackRef}>{items.map(x=><button type="button" key={x.value} className={`visual-carousel-card ${value===x.value?"visual-option-selected":""}`} onClick={()=>onChange(x.value)}><img src={x.image} alt=""/><span>{x.value}</span></button>)}</div><button className="carousel-arrow carousel-arrow-right" onClick={()=>move(1)} type="button">›</button></div></section>}
function CreateAiTattooPage(){const nav=useNavigate();const pRef=useRef(null),sRef=useRef(null);const [form,setForm]=useState({title:"",tattooStyle:"",placement:"",description:""});const [file,setFile]=useState(null),[busy,setBusy]=useState(false),[error,setError]=useState("");
 const submit=async e=>{e.preventDefault();setError("");if(!form.tattooStyle||!form.placement)return setError("Choose a tattoo style and placement.");setBusy(true);try{const p=await createFreeAiProject(form,file);nav(`/ai-studio/${p.id}`);}catch(e){setError(e.message)}finally{setBusy(false)}};
 return <main className="page-shell"><section className="container ai-create-shell"><div className="header"><p className="subtitle">Your free tattoo project</p><h1>Build the creative direction.</h1><p>Style and placement become permanent after the project is created. Improvements stay focused on this tattoo.</p></div><form onSubmit={submit} className="ai-create-form"><div className="ai-direction-card">
  <div className="ai-direction-card-head"><div><p className="subtitle">Creative brief</p><h2>Define the tattoo concept</h2></div><span>Step 1 of 3</span></div>
  <div className="ai-create-fields">
    <label className="ai-field"><span>Project name</span><input value={form.title} onChange={e=>setForm({...form,title:e.target.value})} required placeholder="Wolf forearm concept"/></label>
    <label className={`ai-reference-upload ${file?"has-file":""}`}>
      <input type="file" accept="image/png,image/jpeg,image/webp" onChange={e=>setFile(e.target.files?.[0]||null)}/>
      <span className="ai-upload-icon">＋</span>
      <strong>{file?file.name:"Add a reference image"}</strong>
      <small>{file?"Click to replace the selected file":"PNG, JPG or WebP · optional"}</small>
    </label>
    <label className="ai-field ai-description-field"><span>Describe the tattoo</span><textarea rows="6" value={form.description} onChange={e=>setForm({...form,description:e.target.value})} required placeholder="Describe the main subject, mood, composition, important details and colors..."/><small>Be specific about the subject, direction, number of elements and anything that must not be included.</small></label>
  </div>
</div><Selector title="Body placement" items={PLACEMENTS} value={form.placement} onChange={v=>setForm({...form,placement:v})} trackRef={pRef}/><Selector title="Tattoo style" items={TATTOO_STYLES} value={form.tattooStyle} onChange={v=>setForm({...form,tattooStyle:v})} trackRef={sRef}/>{error&&<p className="error-message">{error}</p>}<button className="primary-button ai-generate-button" disabled={busy}>{busy?"Creating your tattoo concept...":"Generate free concept"}</button></form></section></main>}
export default CreateAiTattooPage;
