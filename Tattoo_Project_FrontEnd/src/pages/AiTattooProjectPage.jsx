import {
  useCallback,
  useEffect,
  useState,
} from "react";
import {
  Link,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
import {
  downloadAiVersion,
  editAiProject,
  generateAiProject,
  getAiProject,
} from "../api/aiTattooApi";
import { getImageUrl } from "../utils/images";
import ImageLightbox from "../components/ImageLightbox";
import { useAuth } from "../context/AuthContext";
import { getUiLocale } from "../i18n/locale";
import { billingPlatform, getAiPassProductDetails, purchaseAiProjectPass, restoreAiProjectPass } from "../services/billingService";
import { useLanguage } from "../i18n/LanguageContext";

function AiTattooProjectPage() {
  const { projectId } = useParams();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { isAdmin } = useAuth();
  const { language } = useLanguage();
  const bg = language === "bg";

  const [project, setProject] = useState(null);
  const [selected, setSelected] = useState(null);
  const [instruction, setInstruction] =
    useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [lightbox, setLightbox] =
    useState(null);
  const [passProduct, setPassProduct] = useState(null);

  useEffect(() => { getAiPassProductDetails().then(setPassProduct).catch(() => {}); }, []);

  const load = useCallback(async () => {
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
  }, [projectId]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const returned = searchParams.get("payment") === "success";
      for (let attempt = 0; attempt < (returned ? 6 : 1); attempt += 1) {
        await load();
        if (!returned || cancelled) break;
        const latest = await getAiProject(projectId);
        if (latest.canEdit) { if (!cancelled) { setProject(latest); setSelected(latest.versions?.at(-1) || null); } break; }
        await new Promise((resolve) => setTimeout(resolve, 1500 * (attempt + 1)));
      }
      if (!cancelled && searchParams.has("payment")) setSearchParams({}, { replace: true });
    })();
    return () => { cancelled = true; };
  }, [load, projectId, searchParams, setSearchParams]);

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

  const unlock = async () => {
    setBusy(true); setError("");
    try {
      const result = await purchaseAiProjectPass(project.id);
      if (result?.pending) setError(bg ? "Покупката чака потвърждение. Не купувай повторно." : "The purchase is pending. Do not purchase again.");
      else if (!result?.redirected) await load();
    } catch (purchaseError) { setError(purchaseError.message); }
    finally { setBusy(false); }
  };

  const restore = async () => {
    setBusy(true); setError("");
    try { await restoreAiProjectPass(project.id); await load(); }
    catch (restoreError) { setError(restoreError.message); }
    finally { setBusy(false); }
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
              {isAdmin
                ? "Admin unlimited access"
                : project.canEdit
                  ? "Paid AI editing available"
                  : project.needsPayment ? "Payment required for improvements" : "Generate the first version"}
            </strong>

            <small>
              {isAdmin
                ? "Project and edit limits are bypassed for development testing"
                : project.canEdit
                  ? `Paid editing access is active${project.editingAccessUntil ? ` until ${new Date(project.editingAccessUntil).toLocaleDateString(getUiLocale())}` : ""}`
                  : project.isFreeProject
                    ? "The free project includes one generation and no free edits"
                    : project.needsPayment ? "Unlock this project before generation and editing" : "Generate the first version to begin"}
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
                    aria-pressed={selected?.id === version.id}
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
                        ).toLocaleString(getUiLocale())}
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
                    !project.canGenerate || busy
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
            ) : project.needsPayment ? (
              <div className="ai-upgrade-box">
                <span>🤖</span>

                <h3>
                  Payment required for improvements
                </h3>

                <p>
                  {project.isFreeProject
                    ? (bg ? "Безплатният проект включва едно изображение и няма безплатни редакции. AI Project Pass е еднократна покупка, важи само за този проект и отключва редакции за 30 дни." : "The free project includes one image and no free edits. AI Project Pass is a one-time purchase valid only for this project and unlocks editing for 30 days.")
                    : (bg ? "Отключи този платен проект за първоначална генерация и редакции за 30 дни." : "Unlock this paid project for its initial generation and 30 days of editing.")}
                </p>
                <button type="button" className="primary-button" disabled={busy} onClick={unlock}>
                  {busy ? (bg ? "Обработване..." : "Processing...") : `${bg ? "Отключи 30 дни за" : "Unlock 30 days for"} ${passProduct?.formattedPrice || "€12.49"}`}
                </button>
                {billingPlatform() !== "web" && <button type="button" className="secondary-button" disabled={busy} onClick={restore}>{bg ? "Възстанови незавършена покупка" : "Restore unfinished purchase"}</button>}
              </div>
            ) : (
              <div className="ai-upgrade-box"><span>🤖</span><h3>Generate the first version</h3><p>After the initial image is ready, editing controls will appear here.</p></div>
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
