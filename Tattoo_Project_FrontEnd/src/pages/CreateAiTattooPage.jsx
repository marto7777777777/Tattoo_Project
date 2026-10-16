import { useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { createFreeAiProject, createPaidAiDraft } from "../api/aiTattooApi";
import { purchaseAiProjectPass } from "../services/billingService";
import {
  PLACEMENTS,
  TATTOO_STYLES,
} from "../data/tattooOptions";
import { useCarouselEdges } from "../hooks/useCarouselEdges";
import { moveCarouselPage } from "../utils/carousel";

import { optimizeImageForUpload } from "../utils/images";

function Selector({
  title,
  items,
  value,
  onChange,
  trackRef,
}) {
  const { canScrollLeft, canScrollRight } = useCarouselEdges(trackRef, items.length);

  const move = (direction) => {
    moveCarouselPage(trackRef.current, direction);
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
        {canScrollLeft && (
          <button
            className="carousel-arrow carousel-arrow-left"
            onClick={() => move(-1)}
            type="button"
            aria-label={`Previous ${title} options`}
          >
            ‹
          </button>
        )}

        <div
          className="selector-carousel-track"
          ref={trackRef}
        >
          {items.map((item) => (
            <button
              type="button"
              key={item.value}
              aria-pressed={value === item.value}
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

        {canScrollRight && (
          <button
            className="carousel-arrow carousel-arrow-right"
            onClick={() => move(1)}
            type="button"
            aria-label={`Next ${title} options`}
          >
            ›
          </button>
        )}
      </div>
    </section>
  );
}

function CreateAiTattooPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const paid = searchParams.get("paid") === "1";

  const placementRef = useRef(null);
  const styleRef = useRef(null);

  const [form, setForm] = useState({
    title: "",
    tattooStyle: "",
    placement: "",
    description: "",
  });

  const imageLock = useRef(false);
  const mounted = useRef(true);
  const [preparingImage, setPreparingImage] = useState(false);
  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  async function chooseReference(event) {
    const input = event.currentTarget;
    const selected = input.files?.[0];
    // Cancel preserves both the reference and the creative brief.
    if (!selected || imageLock.current) return;
    imageLock.current = true;
    setPreparingImage(true);
    setError("");
    try {
      const prepared = await optimizeImageForUpload(selected, { quality: 0.88 });
      if (mounted.current) setFile(prepared);
    } catch (imageError) {
      // Only display known local validation messages, never arbitrary errors.
      const messages = new Set([
        "Please select a valid image.",
        "Only JPG, PNG and WEBP images are supported.",
        "The selected file could not be decoded as an image.",
        "The image has invalid dimensions.",
        "The optimized image is still too large. Please choose a smaller image.",
      ]);
      if (mounted.current) setError(messages.has(imageError?.message)
        ? imageError.message
        : "The image could not be prepared. Choose a valid JPG, PNG or WebP image, or try a smaller image.");
    } finally {
      // Keep the picker-owned File attached through asynchronous decoding.
      // Reset afterwards so the same file can trigger the next change event.
      input.value = "";
      imageLock.current = false;
      if (mounted.current) setPreparingImage(false);
    }
  }

  const [file, setFile] = useState(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const submit = async (event) => {
    event.preventDefault();
    if (busy || imageLock.current) return;
    setError("");

    if (!form.tattooStyle || !form.placement) {
      setError(
        "Choose a tattoo style and placement."
      );
      return;
    }

    setBusy(true);

    try {
      const project = await (paid ? createPaidAiDraft : createFreeAiProject)(
        form,
        file
      );
      if (paid) {
        const purchase = await purchaseAiProjectPass(project.id);
        if (!purchase?.redirected) navigate(`/ai-studio/${project.id}`);
      } else navigate(`/ai-studio/${project.id}`);
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
    <main className="page-shell ai-create-page">
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
                  onChange={chooseReference}
                  disabled={busy || preparingImage}
                  aria-label="Choose a reference image or take a photo"
                />

                <span className="ai-upload-icon">
                  ＋
                </span>

                <strong>
                  {preparingImage ? "Preparing image…" : file
                    ? file.name
                    : "Add a reference image"}
                </strong>

                <small>
                  {file
                    ? "Click to replace the selected file"
                    : "PNG, JPG or WebP · optional"}
                </small>
              </label>

              <p className="muted">If the camera is unavailable or access is denied, enable camera access in Settings or choose an image from your photo library.</p>

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
            disabled={busy || preparingImage}
          >
            {busy
              ? paid ? "Creating paid draft..." : "Creating your tattoo concept..."
              : paid ? "Create project and continue to payment — €12.49" : "Generate tattoo concept"}
          </button>
        </form>
      </section>
    </main>
  );
}

export default CreateAiTattooPage;
