import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  acceptStudioJoinRequest,
  getMyStudio,
  rejectStudioJoinRequest,
  removeStudioMember,
  setStudioOpenForJoinRequests,
  updateMyStudio,
  searchOpenStudiosForJoin,
  requestJoinStudio,
  createMyStudio,
} from "../api/studioApi";
import UserAvatar from "../components/UserAvatar";

const emptyEdit = { name: "", description: "", address: "", city: "", country: "" };

function ArtistWorkspacePage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [editForm, setEditForm] = useState(emptyEdit);
  const [memberToRemove, setMemberToRemove] = useState(null);
  const [noStudioMode, setNoStudioMode] = useState(null);
  const [joinQuery, setJoinQuery] = useState("");
  const [joinResults, setJoinResults] = useState([]);
  const [selectedStudio, setSelectedStudio] = useState(null);
  const [studioCreateForm, setStudioCreateForm] = useState({ name: "", description: "", address: "", city: "", country: "Bulgaria" });

  async function refresh() {
    setLoading(true);
    setError("");
    try {
      const result = await getMyStudio();
      setData(result);
      if (result?.studio) {
        setEditForm({
          name: result.studio.name || "",
          description: result.studio.description || "",
          address: result.studio.address || "",
          city: result.studio.city || "",
          country: result.studio.country || "",
        });
      }
    } catch (err) {
      setError(err.message || "Could not load your studio.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { refresh(); }, []);

  async function doAction(action, successMessage) {
    setError("");
    setSuccess("");
    try {
      await action();
      setSuccess(successMessage);
      await refresh();
    } catch (err) {
      setError(err.message || "Action failed.");
    }
  }

  async function saveStudio(event) {
    event.preventDefault();
    await doAction(
      () => updateMyStudio({
        ...editForm,
        latitude: null,
        longitude: null,
      }),
      "Studio information updated."
    );
    setEditing(false);
  }

  async function searchStudios() {
    setError("");
    if (joinQuery.trim().length < 2) { setJoinResults([]); return; }
    try { setJoinResults(await searchOpenStudiosForJoin(joinQuery)); }
    catch (err) { setError(err.message || "Could not search studios."); }
  }

  async function sendJoinRequest() {
    if (!selectedStudio?.id) { setError("Choose a studio first."); return; }
    await doAction(() => requestJoinStudio(selectedStudio.id), "Join request sent.");
    setSelectedStudio(null); setJoinResults([]); setJoinQuery(""); setNoStudioMode(null);
  }

  async function createStudio(event) {
    event.preventDefault();
    const f = studioCreateForm;
    if (![f.name, f.description, f.address, f.city, f.country].every((x) => x.trim())) { setError("Complete all studio fields."); return; }
    await doAction(() => createMyStudio(f), "Studio created successfully.");
    setNoStudioMode(null);
  }

  if (loading) return <main className="page-shell"><section className="container"><p className="message">Loading your studio...</p></section></main>;

  if (!data?.hasStudio) {
    const pending = data?.pendingJoinRequest;
    return (
      <main className="page-shell"><section className="container">
        <div className="header"><p className="subtitle">My Studio</p><h1>{pending ? "Waiting for studio approval" : "Choose what you want to do"}</h1>
          <p>{pending ? `Your request to join ${pending.studioName} is pending.` : "You currently do not belong to a studio. Create your own studio or join an existing one."}</p></div>
        {error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}
        {pending ? <article className="card form-card pending-studio-card"><p className="subtitle inline-subtitle">Pending request</p><h2>{pending.studioName}</h2><p className="muted">Sent {new Date(pending.createdOn).toLocaleString()}</p><span className="status-badge status-pending">Pending</span></article> : <>
          <div className="studio-choice-grid">
            <button type="button" className={`studio-choice-card ${noStudioMode === "create" ? "selected" : ""}`} onClick={() => { setNoStudioMode("create"); setSelectedStudio(null); }}><strong>Create My Studio</strong><span>Start a new studio and become its owner.</span></button>
            <button type="button" className={`studio-choice-card ${noStudioMode === "join" ? "selected" : ""}`} onClick={() => setNoStudioMode("join")}><strong>Join Studio</strong><span>Find an existing studio and request to join.</span></button>
          </div>
          {noStudioMode === "create" && <form className="card form-card artist-onboarding-form" onSubmit={createStudio}><div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">New studio</p><h2>Studio Information</h2></div></div>
            <label>Studio name<input value={studioCreateForm.name} onChange={(e)=>setStudioCreateForm({...studioCreateForm,name:e.target.value})}/></label>
            <label>Description<textarea rows="4" value={studioCreateForm.description} onChange={(e)=>setStudioCreateForm({...studioCreateForm,description:e.target.value})}/></label>
            <label>Address<input value={studioCreateForm.address} onChange={(e)=>setStudioCreateForm({...studioCreateForm,address:e.target.value})}/></label>
            <label>City<input value={studioCreateForm.city} onChange={(e)=>setStudioCreateForm({...studioCreateForm,city:e.target.value})}/></label>
            <label>Country<input value={studioCreateForm.country} onChange={(e)=>setStudioCreateForm({...studioCreateForm,country:e.target.value})}/></label>
            <button className="primary-button" type="submit">Create Studio</button></form>}
          {noStudioMode === "join" && <section className="card form-card artist-onboarding-form"><div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Existing studio</p><h2>Join Studio</h2></div></div>
            <div className="studio-search-row"><input placeholder="Search by studio name, city or country" value={joinQuery} onChange={(e)=>setJoinQuery(e.target.value)} onKeyDown={(e)=>{if(e.key==="Enter"){e.preventDefault();searchStudios();}}}/><button type="button" className="secondary-button" onClick={searchStudios}>Search</button></div>
            <div className="join-studio-results">{joinResults.map((studio)=><button type="button" key={studio.id} className={`join-studio-result ${selectedStudio?.id===studio.id?"selected":""}`} onClick={()=>setSelectedStudio(studio)}><strong>{studio.name}</strong><span>{studio.city}, {studio.country}</span><small>{studio.description}</small></button>)}</div>
            {selectedStudio && <div className="selected-studio-summary"><strong>{selectedStudio.name}</strong><span>{selectedStudio.address}</span><button type="button" className="primary-button" onClick={sendJoinRequest}>Send Join Request</button></div>}
          </section>}
        </>}
      </section></main>
    );
  }

  const studio = data.studio;
  const artists = studio?.artists || [];

  return (
    <main className="page-shell">
      <section className="container">
        <div className="studio-workspace-hero">
          <div>
            <p className="subtitle">My Studio</p>
            <h1>{studio.name}</h1>
            <p>{studio.description}</p>
            <div className="studio-workspace-meta"><span>{studio.address}</span><span>{studio.city}, {studio.country}</span><span>{artists.length} artist{artists.length === 1 ? "" : "s"}</span></div>
          </div>
          {data.isOwner ? (
            <div className="owner-control-card">
              <span className={`studio-open-state ${studio.isOpenForJoinRequests ? "open" : "closed"}`}>{studio.isOpenForJoinRequests ? "Accepting artists" : "Join requests closed"}</span>
              <button className={studio.isOpenForJoinRequests ? "danger-button" : "secondary-button"} type="button" onClick={() => doAction(() => setStudioOpenForJoinRequests(!studio.isOpenForJoinRequests), studio.isOpenForJoinRequests ? "Studio closed for new join requests." : "Studio is accepting join requests again.")}>{studio.isOpenForJoinRequests ? "Close applications" : "Open applications"}</button>
            </div>
          ) : null}
        </div>

        {error && <p className="error">{error}</p>}
        {success && <p className="success">{success}</p>}

        <div className="filter-tabs studio-tabs">
          <Link className="filter-tab" to="/my-studio/requests">My requests</Link>
          <Link className="filter-tab" to="/my-studio/calendar">My calendar</Link>
        </div>

        <div className="grid-2 studio-workspace-grid">
          <article className="card form-card dashboard-card"><div><p className="subtitle inline-subtitle">Your workflow</p><h2>Requests</h2><p className="muted">Your tattoo requests remain assigned to you personally, not to the whole studio.</p></div><Link className="primary-button" to="/my-studio/requests">Open requests</Link></article>
          <article className="card form-card dashboard-card"><div><p className="subtitle inline-subtitle">Your availability</p><h2>Calendar</h2><p className="muted">Your consultations, sessions and unavailable dates remain separate from every other artist in the studio.</p></div><Link className="primary-button" to="/my-studio/calendar">Open calendar</Link></article>
        </div>

        <section className="card form-card studio-members-section">
          <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Team</p><h2>Studio members</h2><p className="muted">Artists are shown in studio order. Ownership is kept private from clients.</p></div><span className="step-chip">{artists.length}</span></div>
          <div className="studio-member-list">
            {artists.map((artist) => (
              <div className="studio-member-row" key={artist.id}>
                <UserAvatar firstName={artist.firstName} lastName={artist.lastName} imageUrl={artist.profileImageUrl} size="medium" />
                <div className="studio-member-copy"><strong>{artist.firstName} {artist.lastName}</strong><span>{artist.description}</span><small>{artist.phoneNumber}</small></div>
                <div className="studio-member-actions"><span className="rating-chip">★ {artist.averageRating || "New"}</span>{data.isOwner && artist.id !== data.currentArtistId && <button className="danger-button compact-button" type="button" onClick={() => setMemberToRemove(artist)}>Remove</button>}</div>
              </div>
            ))}
          </div>
        </section>

        {data.isOwner && (
          <>
            <section className="card form-card studio-requests-section">
              <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Owner controls</p><h2>Join requests</h2></div><span className="step-chip">{data.pendingRequests?.length || 0}</span></div>
              {(data.pendingRequests || []).length === 0 ? <p className="muted">No pending artist requests.</p> : (data.pendingRequests || []).map((request) => (
                <div className="studio-join-request-row" key={request.id}>
                  <UserAvatar firstName={request.artistName?.split(" ")[0]} lastName={request.artistName?.split(" ").slice(1).join(" ")} imageUrl={request.artistProfileImageUrl} size="medium" />
                  <div><strong>{request.artistName}</strong><p>{request.artistDescription}</p><small>{request.artistPhoneNumber}</small></div>
                  <div className="studio-request-actions"><button className="secondary-button" type="button" onClick={() => doAction(() => rejectStudioJoinRequest(request.id), "Join request rejected.")}>Reject</button><button className="primary-button" type="button" onClick={() => doAction(() => acceptStudioJoinRequest(request.id), "Artist added to studio.")}>Accept</button></div>
                </div>
              ))}
            </section>

            <section className="card form-card studio-edit-section">
              <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Owner controls</p><h2>Studio information</h2></div><button className="secondary-button compact-button" type="button" onClick={() => setEditing((value) => !value)}>{editing ? "Cancel" : "Edit studio"}</button></div>
              {editing ? (
                <form className="form" onSubmit={saveStudio}>
                  <div className="form-group"><label>Name</label><input value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} /></div>
                  <div className="form-group"><label>Description</label><textarea value={editForm.description} onChange={(e) => setEditForm({ ...editForm, description: e.target.value })} /></div>
                  <div className="form-group"><label>Address</label><input value={editForm.address} onChange={(e) => setEditForm({ ...editForm, address: e.target.value })} /></div>
                  <div className="form-row"><div className="form-group"><label>City</label><input value={editForm.city} onChange={(e) => setEditForm({ ...editForm, city: e.target.value })} /></div><div className="form-group"><label>Country</label><input value={editForm.country} onChange={(e) => setEditForm({ ...editForm, country: e.target.value })} /></div></div>
                  <button className="primary-button" type="submit">Save studio</button>
                </form>
              ) : <div className="studio-readonly-info"><strong>{studio.name}</strong><p>{studio.description}</p><span>{studio.address}, {studio.city}, {studio.country}</span></div>}
            </section>
          </>
        )}

        {memberToRemove && (
          <div className="modal-backdrop studio-confirm-backdrop" onClick={() => setMemberToRemove(null)}>
            <section className="modal-card studio-confirm-modal" onClick={(event) => event.stopPropagation()}>
              <div className="studio-confirm-icon">!</div>
              <p className="subtitle inline-subtitle">Confirm removal</p>
              <h2>Remove {memberToRemove.firstName} {memberToRemove.lastName}?</h2>
              <p>This artist will be removed from <strong>{studio.name}</strong>. Their artist profile will be kept, but they will no longer appear publicly until they join or create another studio, but they will no longer be a member of this studio.</p>
              <div className="studio-confirm-actions">
                <button className="secondary-button" type="button" onClick={() => setMemberToRemove(null)}>Cancel</button>
                <button className="danger-button" type="button" onClick={async () => {
                  const artist = memberToRemove;
                  setMemberToRemove(null);
                  await doAction(() => removeStudioMember(artist.id), "Artist removed from studio.");
                }}>Remove artist</button>
              </div>
            </section>
          </div>
        )}
      </section>
    </main>
  );
}

export default ArtistWorkspacePage;
