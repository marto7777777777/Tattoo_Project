import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createFreeAiProject } from "../api/aiTattooApi";
import {
  PLACEMENTS,
  TATTOO_STYLES,
} from "../data/tattooOptions";

function Selector({
  title,
  items,
  value,
  onChange,
  trackRef,
}) {
  const move = (direction) => {
    const element = trackRef.current;

    if (!element) {
      return;
    }

    const card = element.querySelector(
      ".visual-carousel-card"
    );

    if (card) {
      element.scrollBy({
        left:
          direction *
          (card.getBoundingClientRect().width + 12) *
          3,
        behavior: "smooth",
      });
    }
  };

  return (
    <section className="section carousel-section">
      <div className="selection-section-head">
        <div>
          <p className="subtitle">
            Locked after generation
          </p>

          <h2>{title}</h2>
        </div>

        <span className="selection-value">
          {value || "Choose one"}
        </span>
      </div>

      <div className="selector-carousel-shell">
        <button
          className="carousel-arrow carousel-arrow-left"
          onClick={() => move(-1)}
          type="button"
        >
          ‹
        </button>

        <div
          className="selector-carousel-track"
          ref={trackRef}
        >
          {items.map((item) => (
            <button
              type="button"
              key={item.value}
              className={`visual-carousel-card ${
                value === item.value
                  ? "visual-option-selected"
                  : ""
              }`}
              onClick={() => onChange(item.value)}
            >
              <img src={item.image} alt="" />
              <span>{item.value}</span>
            </button>
          ))}
        </div>

        <button
          className="carousel-arrow carousel-arrow-right"
          onClick={() => move(1)}
          type="button"
        >
          ›
        </button>
      </div>
    </section>
  );
}

function CreateAiTattooPage() {
  const navigate = useNavigate();

  const placementRef = useRef(null);
  const styleRef = useRef(null);

  const [form, setForm] = useState({
    title: "",
    tattooStyle: "",
    placement: "",
    description: "",
  });

  const [file, setFile] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const submit = async (event) => {
    event.preventDefault();
    setError("");

    if (!form.tattooStyle || !form.placement) {
      setError(
        "Choose a tattoo style and placement."
      );
      return;
    }

    setBusy(true);

    try {
      const project = await createFreeAiProject(
        form,
        file
      );

      navigate(`/ai-studio/${project.id}`);
    } catch (submitError) {
      setError(
        submitError.message ||
          "The AI tattoo project could not be created."
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="page-shell">
      <section className="container ai-create-shell">
        <div className="header">
          <p className="subtitle">
            AI tattoo project
          </p>

          <h1>Build the creative direction.</h1>

          <p>
            Style and placement become permanent
            after the project is created. Improvements
            stay focused on this tattoo.
          </p>
        </div>

        <form
          onSubmit={submit}
          className="ai-create-form"
        >
          <div className="ai-direction-card">
            <div className="ai-direction-card-head">
              <div>
                <p className="subtitle">
                  Creative brief
                </p>

                <h2>Define the tattoo concept</h2>
              </div>

              <span>Step 1 of 3</span>
            </div>

            <div className="ai-create-fields">
              <label className="ai-field">
                <span>Project name</span>

                <input
                  value={form.title}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      title: event.target.value,
                    })
                  }
                  required
                  placeholder="Wolf forearm concept"
                />
              </label>

              <label
                className={`ai-reference-upload ${
                  file ? "has-file" : ""
                }`}
              >
                <input
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  onChange={(event) =>
                    setFile(
                      event.target.files?.[0] || null
                    )
                  }
                />

                <span className="ai-upload-icon">
                  ＋
                </span>

                <strong>
                  {file
                    ? file.name
                    : "Add a reference image"}
                </strong>

                <small>
                  {file
                    ? "Click to replace the selected file"
                    : "PNG, JPG or WebP · optional"}
                </small>
              </label>

              <label className="ai-field ai-description-field">
                <span>Describe the tattoo</span>

                <textarea
                  rows="6"
                  value={form.description}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      description:
                        event.target.value,
                    })
                  }
                  required
                  placeholder="Describe the main subject, mood, composition, important details and colors..."
                />

                <small>
                  Be specific about the subject,
                  direction, number of elements and
                  anything that must not be included.
                </small>
              </label>
            </div>
          </div>

          <Selector
            title="Body placement"
            items={PLACEMENTS}
            value={form.placement}
            onChange={(value) =>
              setForm({
                ...form,
                placement: value,
              })
            }
            trackRef={placementRef}
          />

          <Selector
            title="Tattoo style"
            items={TATTOO_STYLES}
            value={form.tattooStyle}
            onChange={(value) =>
              setForm({
                ...form,
                tattooStyle: value,
              })
            }
            trackRef={styleRef}
          />

          {error && (
            <p className="error-message">
              {error}
            </p>
          )}

          <button
            className="primary-button ai-generate-button"
            disabled={busy}
          >
            {busy
              ? "Creating your tattoo concept..."
              : "Generate tattoo concept"}
          </button>
        </form>
      </section>
    </main>
  );
}

export default CreateAiTattooPage;