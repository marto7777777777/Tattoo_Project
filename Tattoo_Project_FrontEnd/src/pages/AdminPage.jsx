import { useEffect, useState } from "react";
import {
  deleteAdminAiProject,
  deleteAdminArtistProfile,
  deleteAdminClientProfile,
  deleteAdminTattooRequest,
  deleteAdminUser,
  getAdminAiProjects,
  getAdminOverview,
  getAdminTattooRequests,
  getAdminUsers,
  setAdminArtistVerified,
} from "../api/adminApi";

function AdminPage() {
  const [overview, setOverview] = useState(null);
  const [users, setUsers] = useState([]);
  const [requests, setRequests] = useState([]);
  const [aiProjects, setAiProjects] = useState([]);
  const [tab, setTab] = useState("users");
  const [loading, setLoading] = useState(true);
  const [busyKey, setBusyKey] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [overviewData, usersData, requestsData, aiData] = await Promise.all([
        getAdminOverview(),
        getAdminUsers(),
        getAdminTattooRequests(),
        getAdminAiProjects(),
      ]);
      setOverview(overviewData);
      setUsers(usersData || []);
      setRequests(requestsData || []);
      setAiProjects(aiData || []);
    } catch (err) {
      setError(err.message || "Admin data could not be loaded.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function runAction(key, confirmation, action) {
    if (confirmation && !window.confirm(confirmation)) return;
    setBusyKey(key);
    setError("");
    setSuccess("");
    try {
      await action();
      setSuccess("Admin action completed successfully.");
      await load();
    } catch (err) {
      setError(err.message || "Admin action failed.");
    } finally {
      setBusyKey("");
    }
  }

  if (loading) {
    return <main className="page-shell"><section className="container"><div className="loading-state">Loading admin control center...</div></section></main>;
  }

  return (
    <main className="page-shell admin-page">
      <section className="container">
        <div className="admin-hero">
          <div>
            <p className="subtitle">InkRoute administration</p>
            <h1>Admin Control Center</h1>
            <p>Development administration for accounts, profiles, tattoo requests and AI projects. Destructive actions are permanent.</p>
          </div>
          <span className="admin-badge">ADMIN</span>
        </div>

        {overview && (
          <div className="admin-stat-grid">
            <Stat label="Users" value={overview.users} />
            <Stat label="Client profiles" value={overview.clients} />
            <Stat label="Artist profiles" value={overview.artists} />
            <Stat label="Tattoo requests" value={overview.tattooRequests} />
            <Stat label="AI projects" value={overview.aiProjects} />
          </div>
        )}

        {error && <p className="error-message">{error}</p>}
        {success && <p className="admin-success">{success}</p>}

        <div className="admin-tabs">
          <button className={tab === "users" ? "active" : ""} onClick={() => setTab("users")}>Users & profiles</button>
          <button className={tab === "requests" ? "active" : ""} onClick={() => setTab("requests")}>Tattoo requests</button>
          <button className={tab === "ai" ? "active" : ""} onClick={() => setTab("ai")}>AI projects</button>
        </div>

        {tab === "users" && (
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead>
                <tr><th>User</th><th>Roles</th><th>Profiles</th><th>Requests</th><th>AI</th><th>Actions</th></tr>
              </thead>
              <tbody>
                {users.map((user) => {
                  const isAdmin = user.roles?.includes("Admin");
                  return (
                    <tr key={user.id}>
                      <td><strong>{user.firstName} {user.lastName}</strong><small>{user.email}</small><small>@{user.userName}</small></td>
                      <td><div className="admin-role-list">{user.roles?.map((role) => <span key={role}>{role}</span>)}</div></td>
                      <td><div className="admin-profile-list"><span>Client: {user.clientProfileId ?? "—"}</span><span>Artist: {user.artistProfileId ?? "—"}</span></div></td>
                      <td>{user.tattooRequestCount}</td>
                      <td>{user.aiProjectCount}</td>
                      <td><div className="admin-actions">
                        {user.clientProfileId && <button disabled={busyKey === `client-${user.clientProfileId}`} onClick={() => runAction(`client-${user.clientProfileId}`, `Delete client profile #${user.clientProfileId} and its related client requests?`, () => deleteAdminClientProfile(user.clientProfileId))}>Delete client profile</button>}
                        {user.artistProfileId && <button disabled={busyKey === `artist-${user.artistProfileId}`} onClick={() => runAction(`artist-${user.artistProfileId}`, `Delete artist profile #${user.artistProfileId} and its related studio/request data?`, () => deleteAdminArtistProfile(user.artistProfileId))}>Delete artist profile</button>}
                        {user.artistProfileId && <button disabled={busyKey === `verify-${user.artistProfileId}`} onClick={() => runAction(`verify-${user.artistProfileId}`, null, () => setAdminArtistVerified(user.artistProfileId, true))}>Verify artist</button>}
                        {user.artistProfileId && <button disabled={busyKey === `unverify-${user.artistProfileId}`} onClick={() => runAction(`unverify-${user.artistProfileId}`, null, () => setAdminArtistVerified(user.artistProfileId, false))}>Unverify artist</button>}
                        <button className="danger" disabled={isAdmin || busyKey === `user-${user.id}`} title={isAdmin ? "Admin accounts are protected" : ""} onClick={() => runAction(`user-${user.id}`, `Permanently delete ${user.email} and all related InkRoute data?`, () => deleteAdminUser(user.id))}>Delete user</button>
                      </div></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {tab === "requests" && (
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead><tr><th>ID</th><th>Client</th><th>Artist</th><th>Tattoo</th><th>Status</th><th>Created</th><th>Action</th></tr></thead>
              <tbody>
                {requests.map((request) => (
                  <tr key={request.id}>
                    <td>#{request.id}</td><td>{request.clientName}</td><td>{request.artistName}</td>
                    <td><strong>{request.tattooStyle}</strong><small>{request.placement}</small></td>
                    <td><span className="admin-status">{request.status}</span></td>
                    <td>{new Date(request.createdOn).toLocaleString()}</td>
                    <td><button className="admin-delete-button" disabled={busyKey === `request-${request.id}`} onClick={() => runAction(`request-${request.id}`, `Permanently delete tattoo request #${request.id} and all sessions, consultation, review and response data attached to it?`, () => deleteAdminTattooRequest(request.id))}>Delete request</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {tab === "ai" && (
          <div className="admin-table-wrap">
            <table className="admin-table">
              <thead><tr><th>ID</th><th>User</th><th>Project</th><th>Style / placement</th><th>Versions</th><th>Updated</th><th>Action</th></tr></thead>
              <tbody>
                {aiProjects.map((project) => (
                  <tr key={project.id}>
                    <td>#{project.id}</td><td>{project.userEmail}</td><td><strong>{project.title}</strong></td>
                    <td><span>{project.tattooStyle}</span><small>{project.placement}</small></td>
                    <td>{project.versionCount}</td><td>{new Date(project.updatedAt).toLocaleString()}</td>
                    <td><button className="admin-delete-button" disabled={busyKey === `ai-${project.id}`} onClick={() => runAction(`ai-${project.id}`, `Permanently delete AI project #${project.id} and all generated versions?`, () => deleteAdminAiProject(project.id))}>Delete AI project</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}

function Stat({ label, value }) {
  return <div className="admin-stat-card"><strong>{value ?? 0}</strong><span>{label}</span></div>;
}

export default AdminPage;
