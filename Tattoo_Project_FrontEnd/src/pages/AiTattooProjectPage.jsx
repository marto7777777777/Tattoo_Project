import {
  useEffect,
  useState,
} from "react";
import {
  Link,
  useNavigate,
  useParams,
} from "react-router-dom";
import {
  downloadAiVersion,
  editAiProject,
  generateAiProject,
  getAiProject,
} from "../api/aiTattooApi";
import { getImageUrl } from "../utils/images";
import ImageLightbox from "../components/ImageLightbox";

function AiTattooProjectPage() {
  const { projectId } = useParams();
  const navigate = useNavigate();

  const [project, setProject] = useState(null);
  const [selected, setSelected] = useState(null);
  const [instruction, setInstruction] =
    useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [lightbox, setLightbox] =
    useState(null);

  const load = async () => {
    try {
      const loadedProject =
        await getAiProject(projectId);

      setProject(loadedProject);

      setSelected(
        loadedProject.versions?.at(-1) || null
      );
    } catch (loadError) {
      setError(
        loadError.message ||
          "The AI project could not be loaded."
      );
    }
  };

  useEffect(() => {
    load();
  }, [projectId]);

  const edit = async (event) => {
    event.preventDefault();

    if (!instruction.trim() || !selected) {
      return;
    }

    setBusy(true);
    setError("");

    try {
      const updatedProject =
        await editAiProject(
          project.id,
          instruction.trim(),
          selected.id
        );

      setProject(updatedProject);
      setSelected(
        updatedProject.versions.at(-1)
      );
      setInstruction("");
    } catch (editError) {
      setError(
        editError.message ||
          "The tattoo could not be edited."
      );
    } finally {
      setBusy(false);
    }
  };

  const generate = async () => {
    setBusy(true);
    setError("");

    try {
      const updatedProject =
        await generateAiProject(project.id);

      setProject(updatedProject);
      setSelected(
        updatedProject.versions.at(-1)
      );
    } catch (generateError) {
      setError(
        generateError.message ||
          "The tattoo could not be generated."
      );
    } finally {
      setBusy(false);
    }
  };

  const downloadVersion = async () => {
    if (!selected) {
      return;
    }

    setError("");

    try {
      const blob = await downloadAiVersion(
        selected.id
      );

      const objectUrl =
        URL.createObjectURL(blob);

      const link =
        document.createElement("a");

      const safeTitle =
        project.title
          .replace(/[^a-z0-9]+/gi, "-")
          .replace(/^-|-$/g, "")
          .toLowerCase() || "tattoo";

      link.href = objectUrl;
      link.download =
        `inkroute-${safeTitle}` +
        `-v${selected.versionNumber}.png`;

      document.body.appendChild(link);
      link.click();
      link.remove();

      URL.revokeObjectURL(objectUrl);
    } catch (downloadError) {
      setError(
        downloadError.message ||
          "The image could not be downloaded."
      );
    }
  };

  const useInRequest = () => {
    if (!selected) {
      return;
    }

    localStorage.setItem(
      "aiTattooReference",
      JSON.stringify({
        imageUrl: selected.imageUrl,
        description:
          project.initialDescription,
        tattooStyle: project.tattooStyle,
        placement: project.placement,
        projectId: project.id,
      })
    );

    navigate("/explore");
  };

  if (!project) {
    return (
      <main className="page-shell">
        <section className="container">
          <div className="loading-state">
            Loading AI project...
          </div>

          {error && (
            <p className="error-message">
              {error}
            </p>
          )}
        </section>
      </main>
    );
  }

  return (
    <main className="page-shell ai-editor-page">
      <section className="container">
        <div className="ai-editor-topbar">
          <div>
            <Link
              to="/ai-studio"
              className="back-link"
            >
              ← AI Studio
            </Link>

            <h1>{project.title}</h1>

            <div className="ai-lock-chips">
              <span>
                🔒 {project.tattooStyle}
              </span>

              <span>
                🔒 {project.placement}
              </span>
            </div>
          </div>

          <div
            className={`ai-access-card ${
              project.canEdit
                ? "active"
                : "paused"
            }`}
          >
            <strong>
              {project.canEdit
                ? "AI editing available"
                : "Free edit limit reached"}
            </strong>

            <small>
              {project.canEdit
                ? `${project.freeEditsRemaining} improvements remaining`
                : "Both free improvements have been used"}
            </small>
          </div>
        </div>

        {error && (
          <p className="error-message">
            {error}
          </p>
        )}

        <div className="ai-editor-layout">
          <aside className="ai-version-panel">
            <div className="section-heading">
              <div>
                <p className="subtitle">
                  History
                </p>

                <h2>Versions</h2>
              </div>
            </div>

            <div className="ai-version-list">
              {project.versions.map(
                (version) => (
                  <button
                    type="button"
                    className={
                      selected?.id === version.id
                        ? "active"
                        : ""
                    }
                    key={version.id}
                    onClick={() =>
                      setSelected(version)
                    }
                  >
                    <img
                      src={getImageUrl(
                        version.imageUrl
                      )}
                      alt=""
                    />

                    <span>
                      <strong>
                        Version{" "}
                        {version.versionNumber}
                      </strong>

                      <small>
                        {new Date(
                          version.createdAt
                        ).toLocaleString()}
                      </small>
                    </span>
                  </button>
                )
              )}
            </div>
          </aside>

          <section className="ai-canvas-panel">
            {selected ? (
              <>
                <button
                  type="button"
                  className="ai-main-image"
                  onClick={() =>
                    setLightbox(
                      getImageUrl(
                        selected.imageUrl
                      )
                    )
                  }
                >
                  <img
                    src={getImageUrl(
                      selected.imageUrl
                    )}
                    alt="Generated tattoo concept"
                  />

                  <span>View full size</span>
                </button>

                <div className="ai-canvas-actions">
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={downloadVersion}
                  >
                    Download version
                  </button>

                  <button
                    type="button"
                    className="primary-button"
                    onClick={useInRequest}
                  >
                    Use in tattoo request
                  </button>
                </div>
              </>
            ) : (
              <div className="ai-awaiting-card">
                <h2>
                  Generate the first version
                </h2>

                <p>
                  Start the AI generation for this
                  tattoo project.
                </p>

                <button
                  type="button"
                  className="primary-button"
                  disabled={
                    !project.canEdit || busy
                  }
                  onClick={generate}
                >
                  {busy
                    ? "Generating..."
                    : "Generate first version"}
                </button>
              </div>
            )}
          </section>

          <aside className="ai-chat-panel">
            <div>
              <p className="subtitle">
                Tattoo refinement
              </p>

              <h2>Improve this concept</h2>

              <p>
                Describe one focused change. The AI
                will keep this as tattoo artwork only
                — no arm, skin or body mockup. Style
                and placement remain locked.
              </p>

              <div className="ai-prompt-tips">
                <span>Try:</span>

                <button
                  type="button"
                  onClick={() =>
                    setInstruction(
                      "Keep the same tattoo, remove any body or skin and show only the isolated tattoo design on a plain background."
                    )
                  }
                >
                  Isolate the design
                </button>

                <button
                  type="button"
                  onClick={() =>
                    setInstruction(
                      "Keep the same composition, simplify small details and make the tattoo stencil-ready."
                    )
                  }
                >
                  Simplify details
                </button>

                <button
                  type="button"
                  onClick={() =>
                    setInstruction(
                      "Keep the same subject and style, improve symmetry, line clarity and tattoo readability."
                    )
                  }
                >
                  Improve readability
                </button>
              </div>
            </div>

            {project.canEdit && selected ? (
              <form onSubmit={edit}>
                <textarea
                  rows="7"
                  value={instruction}
                  onChange={(event) =>
                    setInstruction(
                      event.target.value
                    )
                  }
                  placeholder="Add peonies around the main subject, keep the composition vertical..."
                />

                <button
                  className="primary-button"
                  disabled={busy}
                >
                  {busy
                    ? "Creating version..."
                    : "Create improvement"}
                </button>
              </form>
            ) : (
              <div className="ai-upgrade-box">
                <span>🤖</span>

                <h3>
                  Free edit limit reached
                </h3>

                <p>
                  This project has used both free improvements. No additional edits are available.
                </p>
              </div>
            )}
          </aside>
        </div>
      </section>

      <ImageLightbox
        imageUrl={lightbox}
        alt="AI tattoo version"
        onClose={() => setLightbox(null)}
      />
    </main>
  );
}

export default AiTattooProjectPage;