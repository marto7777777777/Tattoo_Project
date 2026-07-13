import { useMemo, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { createTattooRequestWithImages } from "../api/tattooRequestApi";
import { readResponse } from "../api/http";

import outerForearmImage from "../assets/body-placements/outer-forearm.jpeg";
import forearmImage from "../assets/body-placements/forearm.jpeg";
import innerBicepsImage from "../assets/body-placements/inner-biceps.jpeg";
import upperArmImage from "../assets/body-placements/upper-arm.jpeg";
import wristImage from "../assets/body-placements/wrist.jpeg";
import fullChestImage from "../assets/body-placements/full-chest.jpeg";
import chestOneSideImage from "../assets/body-placements/chest-one-side.jpeg";
import halfAbdomenImage from "../assets/body-placements/half-abdomen.jpeg";
import fullAbdomenImage from "../assets/body-placements/full-abdomen.jpeg";
import ribsImage from "../assets/body-placements/ribs.jpeg";
import upperBackImage from "../assets/body-placements/upper-back.jpeg";
import lowerBackImage from "../assets/body-placements/lower-back.jpeg";
import halfBackImage from "../assets/body-placements/half-back.jpeg";
import fullBackImage from "../assets/body-placements/full-back.jpeg";
import gluteImage from "../assets/body-placements/glute.jpeg";
import frontThighImage from "../assets/body-placements/front-thigh.jpeg";
import backThighImage from "../assets/body-placements/back-thigh.jpeg";
import calfImage from "../assets/body-placements/calf.jpeg";
import outerShinImage from "../assets/body-placements/outer-shin.jpeg";
import topFootImage from "../assets/body-placements/top-foot.jpeg";
import fullLegImage from "../assets/body-placements/full-leg.jpeg";
import neckImage from "../assets/body-placements/neck.jpeg";
import faceImage from "../assets/body-placements/face.jpeg";

import fineLineStyleImage from "../assets/tattoo-styles/fine-line.jpg";
import realismStyleImage from "../assets/tattoo-styles/realism.jpg";
import blackworkStyleImage from "../assets/tattoo-styles/blackwork.jpg";
import japaneseStyleImage from "../assets/tattoo-styles/japanese-irezumi.jpg";
import traditionalStyleImage from "../assets/tattoo-styles/american-traditional.jpg";
import neoTraditionalStyleImage from "../assets/tattoo-styles/neo-traditional.jpg";
import watercolorStyleImage from "../assets/tattoo-styles/watercolor.jpg";
import letteringStyleImage from "../assets/tattoo-styles/lettering-script.jpg";
import geometricStyleImage from "../assets/tattoo-styles/geometric.jpg";
import tribalStyleImage from "../assets/tattoo-styles/tribal-polynesian.jpg";
import dotworkStyleImage from "../assets/tattoo-styles/dotwork.jpg";
import illustrativeStyleImage from "../assets/tattoo-styles/illustrative.jpg";
import chicanoStyleImage from "../assets/tattoo-styles/chicano.jpg";
import newSchoolStyleImage from "../assets/tattoo-styles/new-school.jpg";
import biomechanicalStyleImage from "../assets/tattoo-styles/biomechanical.jpg";
import trashPolkaStyleImage from "../assets/tattoo-styles/trash-polka.jpg";
import portraitStyleImage from "../assets/tattoo-styles/portrait.jpg";
import minimalistStyleImage from "../assets/tattoo-styles/minimalist.jpg";

const PLACEMENTS = [
  { value: "Outer Forearm", image: outerForearmImage },
  { value: "Forearm", image: forearmImage },
  { value: "Inner Biceps", image: innerBicepsImage },
  { value: "Upper Arm", image: upperArmImage },
  { value: "Wrist", image: wristImage },
  { value: "Full Chest", image: fullChestImage },
  { value: "Chest (One Side)", image: chestOneSideImage },
  { value: "Half Abdomen (One Side)", image: halfAbdomenImage },
  { value: "Full Abdomen", image: fullAbdomenImage },
  { value: "Ribs", image: ribsImage },
  { value: "Upper Back", image: upperBackImage },
  { value: "Lower Back", image: lowerBackImage },
  { value: "Half Back (One Side)", image: halfBackImage },
  { value: "Full Back", image: fullBackImage },
  { value: "Glute (One Side)", image: gluteImage },
  { value: "Front Thigh", image: frontThighImage },
  { value: "Back Thigh", image: backThighImage },
  { value: "Calf", image: calfImage },
  { value: "Outer Shin", image: outerShinImage },
  { value: "Top of Foot", image: topFootImage },
  { value: "Full Leg", image: fullLegImage },
  { value: "Neck", image: neckImage },
  { value: "Face", image: faceImage },
];

const TATTOO_STYLES = [
  { value: "Fine Line", image: fineLineStyleImage },
  { value: "Realism", image: realismStyleImage },
  { value: "Blackwork", image: blackworkStyleImage },
  { value: "Japanese / Irezumi", image: japaneseStyleImage },
  { value: "American Traditional", image: traditionalStyleImage },
  { value: "Neo Traditional", image: neoTraditionalStyleImage },
  { value: "Watercolor", image: watercolorStyleImage },
  { value: "Lettering / Script", image: letteringStyleImage },
  { value: "Geometric", image: geometricStyleImage },
  { value: "Tribal / Polynesian", image: tribalStyleImage },
  { value: "Dotwork", image: dotworkStyleImage },
  { value: "Illustrative", image: illustrativeStyleImage },
  { value: "Chicano", image: chicanoStyleImage },
  { value: "New School", image: newSchoolStyleImage },
  { value: "Biomechanical", image: biomechanicalStyleImage },
  { value: "Trash Polka", image: trashPolkaStyleImage },
  { value: "Portrait", image: portraitStyleImage },
  { value: "Minimalist", image: minimalistStyleImage },
];

function HorizontalSelector({ title, subtitle, value, children, scrollRef }) {
  function scroll(direction) {
    const track = scrollRef.current;
    if (!track) return;

    const firstCard = track.querySelector(".visual-carousel-card");
    if (!firstCard) return;

    const styles = window.getComputedStyle(track);
    const gap = Number.parseFloat(styles.columnGap || styles.gap || "0");
    const pageWidth = (firstCard.getBoundingClientRect().width + gap) * 3;

    track.scrollBy({ left: direction * pageWidth, behavior: "smooth" });
  }

  return (
    <section className="section carousel-section">
      <div className="selection-section-head">
        <div><p className="subtitle">{subtitle}</p><h2>{title}</h2></div>
        <span className="selection-value">{value || "Choose one"}</span>
      </div>
      <div className="selector-carousel-shell">
        <button className="carousel-arrow carousel-arrow-left" type="button" aria-label="Previous options" onClick={() => scroll(-1)}>‹</button>
        <div className="selector-carousel-track" ref={scrollRef}>{children}</div>
        <button className="carousel-arrow carousel-arrow-right" type="button" aria-label="Next options" onClick={() => scroll(1)}>›</button>
      </div>
    </section>
  );
}

function CreateTattooRequestPage() {
  const navigate = useNavigate();
  const params = useParams();
  const placementTrackRef = useRef(null);
  const styleTrackRef = useRef(null);
  const storedArtist = JSON.parse(localStorage.getItem("selectedArtist") || "null");
  const artistId = params.artistId || storedArtist?.id || storedArtist?.tattooArtistId || "";
  const [form, setForm] = useState({ description: "", placement: "", tattooStyle: "" });
  const [imageFiles, setImageFiles] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const previews = useMemo(
    () => imageFiles.map((file) => ({ file, url: URL.createObjectURL(file) })),
    [imageFiles]
  );

  function handleImagesChange(event) {
    const files = Array.from(event.target.files || []);
    setImageFiles((current) => [...current, ...files]);
    event.target.value = "";
  }

  function removeImage(index) {
    setImageFiles((current) => current.filter((_, itemIndex) => itemIndex !== index));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    if (!artistId) return setError("Please choose an artist before creating a tattoo request.");
    if (!form.placement) return setError("Please choose a body placement.");
    if (!form.tattooStyle) return setError("Please choose a tattoo style.");

    try {
      const response = await createTattooRequestWithImages(
        {
          tattooArtistId: Number(artistId),
          description: form.description,
          placement: form.placement,
          tattooStyle: form.tattooStyle,
        },
        imageFiles
      );
      const data = await readResponse(response);

      if (!response.ok) {
        const message = typeof data === "string" ? data : JSON.stringify(data);
        if (message.includes("Client profile")) {
          navigate(`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(`/create-tattoo-request/${artistId}`)}`);
          return;
        }
        setError(message);
        return;
      }

      setSuccess("Tattoo request created successfully.");
      setTimeout(() => navigate("/bookings"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  if (!artistId) {
    return (
      <main className="center-container">
        <section className="card form-card">
          <div className="header"><p className="subtitle">Tattoo Request</p><h1>Choose an artist first</h1><p>Open Explore and select the artist you want to contact.</p></div>
          <Link className="primary-button" to="/explore">Browse Artists</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="center-container create-request-page">
      <section className="request-layout create-request-layout">
        <aside className="card side-panel create-request-artist-panel">
          <p className="subtitle">Selected Artist</p>
          <h2>{storedArtist?.studioName || "Artist selected"}</h2>
          <p className="muted">{storedArtist ? `${storedArtist.firstName} ${storedArtist.lastName}` : "You selected this artist from Explore."}</p>
          <div className="info-list">
            {storedArtist?.studioAddress && <p><span>Studio:</span> {storedArtist.studioAddress}</p>}
            {(storedArtist?.studioCity || storedArtist?.studioCountry) && <p><span>Location:</span> {[storedArtist.studioCity, storedArtist.studioCountry].filter(Boolean).join(", ")}</p>}
            {storedArtist?.consultationDurationMinutes && <p><span>Consultation:</span> {storedArtist.consultationDurationMinutes} minutes</p>}
          </div>
        </aside>

        <section className="card form-card request-form-wide create-request-form">
          <div className="header"><p className="subtitle">Tattoo Request</p><h1>Describe your tattoo idea</h1><p>Choose the placement and style, then add your description and reference images.</p></div>
          <form className="form" onSubmit={handleSubmit}>
            <HorizontalSelector title="Where do you want it?" subtitle="Placement" value={form.placement} scrollRef={placementTrackRef}>
              {PLACEMENTS.map((option) => (
                <button key={option.value} type="button" className={`visual-carousel-card ${form.placement === option.value ? "visual-option-selected" : ""}`} onClick={() => setForm((current) => ({ ...current, placement: option.value }))}>
                  <img src={option.image} alt={option.value} loading="lazy" />
                  <span>{option.value}</span>
                </button>
              ))}
            </HorizontalSelector>

            <HorizontalSelector title="Pick a style" subtitle="Tattoo style" value={form.tattooStyle} scrollRef={styleTrackRef}>
              {TATTOO_STYLES.map((style) => (
                <button key={style.value} type="button" className={`visual-carousel-card ${form.tattooStyle === style.value ? "visual-option-selected" : ""}`} onClick={() => setForm((current) => ({ ...current, tattooStyle: style.value }))}>
                  <img src={style.image} alt={style.value} loading="lazy" />
                  <span>{style.value}</span>
                </button>
              ))}
            </HorizontalSelector>

            <div className="form-group"><label>Description</label><textarea name="description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} required /></div>

            <section className="section">
              <h2>Reference images</h2>
              <label className="portfolio-upload-tile request-upload-zone"><input type="file" accept="image/*" multiple hidden onChange={handleImagesChange} /><span className="upload-zone-icon">＋</span><strong>Add reference images</strong><small>Click to browse JPG, PNG or WEBP files</small></label>
              {previews.length > 0 && <div className="portfolio-manage-grid">{previews.map((preview, index) => <div className="portfolio-manage-card" key={`${preview.file.name}-${index}`}><img src={preview.url} alt="Tattoo reference preview" /><button className="danger-button compact-button" type="button" onClick={() => removeImage(index)}>Remove</button></div>)}</div>}
            </section>

            {error && <p className="error">{error}</p>}
            {success && <p className="success">{success}</p>}
            <button className="primary-button">Send Tattoo Request</button>
          </form>
        </section>
      </section>
    </main>
  );
}

export default CreateTattooRequestPage;
