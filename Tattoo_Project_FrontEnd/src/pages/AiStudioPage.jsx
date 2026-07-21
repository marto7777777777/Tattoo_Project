import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAiProjects } from "../api/aiTattooApi";
import { getImageUrl } from "../utils/images";

function AiStudioPage() {
  const [projects, setProjects] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    getAiProjects().then(setProjects).catch((e) => setError(e.message));
  }, []);

  const freeUsed = projects.some((project) => project.isFreeProject);

  return (
    <main className="page-shell ai-studio-page">
      <section className="container">
        <div className="ai-studio-hero">
          <div>
            <p className="subtitle">InkRoute AI Studio</p>
            <h1>Shape the tattoo before the first session.</h1>
            <p>Create one free tattoo concept, refine it twice, then send it directly into the InkRoute workflow.</p>
          </div>

          {freeUsed ? (
            <button className="primary-button" type="button" disabled title="The one free AI project has already been used">
              Free project used
            </button>
          ) : (
            <Link className="primary-button" to="/ai-studio/new">Create free project</Link>
          )}
        </div>

        <div className="ai-benefit-strip">
          <span><strong>1 free project</strong><small>Initial generation included</small></span>
          <span><strong>2 free improvements</strong><small>Then editing stops</small></span>
          <span><strong>Locked direction</strong><small>Style and placement stay fixed</small></span>
          <span><strong>Download versions</strong><small>Keep every generated result</small></span>
        </div>

        {error && <p className="error-message">{error}</p>}

        <div className="section-heading">
          <div><p className="subtitle">Your workspace</p><h2>AI tattoo projects</h2></div>
        </div>

        <div className="ai-project-grid">
          {projects.map((project) => {
            const last = project.versions?.at(-1);
            return (
              <Link className="ai-project-card" to={`/ai-studio/${project.id}`} key={project.id}>
                <div className="ai-project-cover">
                  {last ? <img src={getImageUrl(last.imageUrl)} alt="" /> : <span>No generated version</span>}
                  <em className={project.canEdit ? "active" : "paused"}>
                    {project.canEdit ? "Active" : "Limit reached"}
                  </em>
                </div>
                <div>
                  <h3>{project.title}</h3>
                  <p>{project.tattooStyle} · {project.placement}</p>
                  <small>{project.versions?.length || 0} versions</small>
                </div>
              </Link>
            );
          })}

          {projects.length === 0 && (
            <div className="empty-state">
              <h3>Your first concept starts here.</h3>
              <p>Create one free project with two AI improvements.</p>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}

export default AiStudioPage;
