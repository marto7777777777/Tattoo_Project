import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createArtistProfile } from "../api/artistApi";
import { addPortfolioImage, updateProfileImage } from "../api/profileApi";
import { searchOpenStudiosForJoin } from "../api/studioApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";
import { WeeklyScheduleBuilder } from "../components/WeeklyScheduleBuilder";

const DEFAULT_AVAILABILITY = {
  consultation: [
    { id: "consultation-default", days: [1, 2, 3, 4, 5], startTime: "10:00", endTime: "12:00" },
  ],
  tattoo: [
    { id: "tattoo-default", days: [1, 2, 3, 4, 5], startTime: "13:00", endTime: "18:00" },
  ],
};

function uniqueFiles(existingFiles, incomingFiles) {
  const files = [...existingFiles];
  const seen = new Set(existingFiles.map((file) => `${file.name}-${file.size}-${file.lastModified}`));

  for (const file of incomingFiles) {
    const key = `${file.name}-${file.size}-${file.lastModified}`;
    if (!seen.has(key)) {
      seen.add(key);
      files.push(file);
    }
  }

  return files;
}

function CreateArtistProfilePage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();

  const [setupMode, setSetupMode] = useState(null); // 0 = create studio, 1 = join studio
  const [artistForm, setArtistForm] = useState({
    description: "",
    phoneNumber: "",
    consultationDurationMinutes: "30",
    offersOnlineConsultation: false,
    requiresDeposit: false,
    depositAmount: "",
    requirements: [""],
  });
  const [studioForm, setStudioForm] = useState({
    name: "",
    description: "",
    address: "",
    city: "",
    country: "Bulgaria",
  });
  const [availability, setAvailability] = useState(DEFAULT_AVAILABILITY);

  const [joinQuery, setJoinQuery] = useState("");
  const [joinResults, setJoinResults] = useState([]);
  const [selectedStudio, setSelectedStudio] = useState(null);
  const [searchingStudios, setSearchingStudios] = useState(false);
  const [profileImageFile, setProfileImageFile] = useState(null);
  const [portfolioImageFiles, setPortfolioImageFiles] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const profilePreview = useMemo(
    () => (profileImageFile ? URL.createObjectURL(profileImageFile) : null),
    [profileImageFile]
  );

  const portfolioPreviews = useMemo(
    () => portfolioImageFiles.map((file) => ({ file, url: URL.createObjectURL(file) })),
    [portfolioImageFiles]
  );

  useEffect(() => () => {
    if (profilePreview) URL.revokeObjectURL(profilePreview);
  }, [profilePreview]);

  useEffect(() => () => {
    portfolioPreviews.forEach((preview) => URL.revokeObjectURL(preview.url));
  }, [portfolioPreviews]);

  function setArtistField(name, value) {
    setArtistForm((current) => ({ ...current, [name]: value }));
  }

  function setStudioField(name, value) {
    setStudioForm((current) => ({ ...current, [name]: value }));
  }

  function updateRequirement(index, value) {
    setArtistForm((current) => {
      const requirements = [...current.requirements];
      requirements[index] = value;
      return { ...current, requirements };
    });
  }

  function addRequirement() {
    setArtistForm((current) => ({ ...current, requirements: [...current.requirements, ""] }));
  }

  function removeRequirement(index) {
    setArtistForm((current) => ({
      ...current,
      requirements: current.requirements.filter((_, currentIndex) => currentIndex !== index),
    }));
  }

  function addPortfolioFiles(fileList) {
    const incoming = Array.from(fileList || []).filter((file) => file.type.startsWith("image/"));
    if (incoming.length === 0) return;
    setPortfolioImageFiles((current) => uniqueFiles(current, incoming));
  }

  function removePortfolioFile(index) {
    setPortfolioImageFiles((current) => current.filter((_, currentIndex) => currentIndex !== index));
  }

  function buildSchedules() {
    const mapBlocks = (blocks, scheduleType) => blocks.flatMap((block) =>
      block.days.map((dayOfWeek) => ({
        dayOfWeek,
        startTime: block.startTime.length === 5 ? `${block.startTime}:00` : block.startTime,
        endTime: block.endTime.length === 5 ? `${block.endTime}:00` : block.endTime,
        scheduleType,
      }))
    );

    return [
      ...mapBlocks(availability.consultation, 1),
      ...mapBlocks(availability.tattoo, 0),
    ];
  }



  async function handleStudioSearch(event) {
    event?.preventDefault();
    setError("");
    setSelectedStudio(null);
    if (joinQuery.trim().length < 2) {
      setJoinResults([]);
      setError("Write at least 2 characters to search for a studio.");
      return;
    }

    try {
      setSearchingStudios(true);
      const data = await searchOpenStudiosForJoin(joinQuery);
      setJoinResults(Array.isArray(data) ? data : []);
    } catch (err) {
      setJoinResults([]);
      setError(err.message || "Could not search studios.");
    } finally {
      setSearchingStudios(false);
    }
  }

  function validateAvailability() {
    const groups = [
      { key: "consultation", label: "consultation" },
      { key: "tattoo", label: "tattoo session" },
    ];

    for (const group of groups) {
      const blocks = availability[group.key];
      if (blocks.length === 0 || !blocks.some((block) => block.days.length > 0)) {
        return `Select at least one working day for ${group.label} availability.`;
      }

      for (const block of blocks) {
        if (block.days.length === 0) continue;
        if (!block.startTime || !block.endTime) return `Choose start and end time for every active ${group.label} block.`;
        if (block.startTime >= block.endTime) return `${group.label[0].toUpperCase()}${group.label.slice(1)} start time must be before end time.`;
      }
    }

    const schedules = buildSchedules();
    for (const day of [0, 1, 2, 3, 4, 5, 6]) {
      const daySchedules = schedules.filter((schedule) => schedule.dayOfWeek === day).sort((a, b) => a.startTime.localeCompare(b.startTime));
      for (let i = 1; i < daySchedules.length; i += 1) {
        if (daySchedules[i].startTime < daySchedules[i - 1].endTime) {
          return "Working hours cannot overlap on the same day. Consultations and tattoo sessions cannot overlap either.";
        }
      }
    }
    const uniqueKeys = new Set();
    for (const schedule of schedules) {
      const key = `${schedule.dayOfWeek}-${schedule.startTime}-${schedule.endTime}-${schedule.scheduleType}`;
      if (uniqueKeys.has(key)) return "The schedule contains a duplicate time block.";
      uniqueKeys.add(key);
    }

    return null;
  }

  function validate() {
    if (setupMode !== 0 && setupMode !== 1) return "Choose Create My Studio or Join Studio first.";
    if (!artistForm.description.trim()) return "Artist description is required.";
    if (!artistForm.phoneNumber.trim()) return "Phone number is required.";

    const duration = Number(artistForm.consultationDurationMinutes);
    if (!Number.isFinite(duration) || duration < 15 || duration > 180) {
      return "Consultation duration must be between 15 and 180 minutes.";
    }

    if (artistForm.requiresDeposit) {
      const deposit = Number(artistForm.depositAmount);
      if (!Number.isFinite(deposit) || deposit <= 0) return "Deposit amount must be greater than 0.";
    }

    const availabilityError = validateAvailability();
    if (availabilityError) return availabilityError;

    if (setupMode === 0) {
      if (!studioForm.name.trim()) return "Studio name is required.";
      if (!studioForm.description.trim()) return "Studio description is required.";
      if (!studioForm.address.trim()) return "Studio address is required.";
      if (!studioForm.city.trim()) return "Studio city is required.";
      if (!studioForm.country.trim()) return "Studio country is required.";
    }

    if (setupMode === 1 && !selectedStudio?.id) return "Search for a studio and select the one you want to join.";
    return null;
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    const payload = {
      description: artistForm.description.trim(),
      phoneNumber: artistForm.phoneNumber.trim(),
      consultationDurationMinutes: Number(artistForm.consultationDurationMinutes),
      offersOnlineConsultation: Boolean(artistForm.offersOnlineConsultation),
      requiresDeposit: Boolean(artistForm.requiresDeposit),
      depositAmount: artistForm.requiresDeposit ? Number(artistForm.depositAmount) : null,
      studioSetupMode: setupMode,
      studio: setupMode === 0 ? {
        name: studioForm.name.trim(),
        description: studioForm.description.trim(),
        address: studioForm.address.trim(),
        city: studioForm.city.trim(),
        country: studioForm.country.trim(),
        latitude: null,
        longitude: null,
      } : null,
      joinStudioId: setupMode === 1 ? Number(selectedStudio.id) : null,
      requirements: artistForm.requirements
        .map((description) => description.trim())
        .filter(Boolean)
        .map((description) => ({ description })),
      portfolioImages: [],
      schedules: buildSchedules(),
    };

    try {
      setSubmitting(true);
      const response = await createArtistProfile(payload);
      const data = await readResponse(response);
      if (!response.ok) {
        setError(typeof data === "string" ? data : data?.title || data?.message || JSON.stringify(data));
        return;
      }

      const token = data?.token || data?.Token;
      if (token) saveAuthToken(token);

      if (profileImageFile) await updateProfileImage(profileImageFile);
      for (const file of portfolioImageFiles) await addPortfolioImage(file);

      if (setupMode === 0) {
        setSuccess("Artist profile and studio created successfully.");
      } else {
        setSuccess(`Artist profile created. Your join request was sent to ${selectedStudio.name}.`);
      }
      setTimeout(() => navigate("/my-studio"), 700);
    } catch (err) {
      setError(err.message || "Server connection failed. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }


  return (
    <main className="page-shell artist-onboarding-page">
      <section className="container artist-onboarding-container">
        <div className="header artist-onboarding-header">
          <p className="subtitle">Tattoo artist profile</p>
          <h1>Set up your artist workspace</h1>
          <p>Create your own studio or request to join an existing studio. Your portfolio, phone, description and working hours always stay attached to your personal artist profile.</p>
        </div>

        <div className="studio-mode-grid">
          <button
            type="button"
            className={`studio-mode-card ${setupMode === 0 ? "selected" : ""}`}
            onClick={() => { setSetupMode(0); setSelectedStudio(null); setError(""); }}
          >
            <span className="studio-mode-icon">＋</span>
            <strong>Create My Studio</strong>
            <small>Create a new studio and become its owner. You can accept artists and manage members later.</small>
          </button>

          <button
            type="button"
            className={`studio-mode-card ${setupMode === 1 ? "selected" : ""}`}
            onClick={() => { setSetupMode(1); setError(""); }}
          >
            <span className="studio-mode-icon">⌕</span>
            <strong>Join Studio</strong>
            <small>Find an existing studio and send a join request. The studio owner must approve you.</small>
          </button>
        </div>

        {setupMode === null && (
          <div className="studio-mode-hint">Choose how you work before filling in the profile.</div>
        )}

        {setupMode !== null && (
          <form className="artist-onboarding-form" onSubmit={handleSubmit}>
            <section className="card form-card onboarding-section">
              <div className="onboarding-section-head">
                <div><p className="subtitle inline-subtitle">Personal profile</p><h2>Your artist information</h2></div>
                <span className="step-chip">1</span>
              </div>

              <div className="profile-create-upload">
                <label className="avatar-upload-label">
                  <input type="file" accept="image/*" hidden onChange={(event) => setProfileImageFile(event.target.files?.[0] || null)} />
                  <div className="user-avatar user-avatar-xlarge">
                    {profilePreview ? <img src={profilePreview} alt="Profile preview" /> : <span>＋</span>}
                  </div>
                  <span>Optional profile picture</span>
                </label>
              </div>

              <div className="form-group">
                <label>Artist description</label>
                <textarea value={artistForm.description} onChange={(event) => setArtistField("description", event.target.value)} placeholder="Tell clients about your tattoo style, experience and the work you enjoy doing." maxLength={1200} />
              </div>
              <div className="artist-info-stack">
                <div className="form-group">
                  <label>Phone number</label>
                  <input value={artistForm.phoneNumber} onChange={(event) => setArtistField("phoneNumber", event.target.value)} placeholder="+359 ..." maxLength={40} />
                </div>

                <label className={`onboarding-toggle-card onboarding-toggle-card-single ${artistForm.offersOnlineConsultation ? "active" : ""}`}>
                  <input type="checkbox" checked={artistForm.offersOnlineConsultation} onChange={(event) => setArtistField("offersOnlineConsultation", event.target.checked)} />
                  <span><strong>Offers online consultation</strong><small>Allow clients to book consultations remotely.</small></span>
                </label>

                <div className="form-group">
                  <label>Consultation duration (minutes)</label>
                  <input type="number" min="15" max="180" value={artistForm.consultationDurationMinutes} onChange={(event) => setArtistField("consultationDurationMinutes", event.target.value)} />
                </div>

                <label className={`onboarding-toggle-card onboarding-toggle-card-single ${artistForm.requiresDeposit ? "active" : ""}`}>
                  <input type="checkbox" checked={artistForm.requiresDeposit} onChange={(event) => setArtistField("requiresDeposit", event.target.checked)} />
                  <span><strong>Requires deposit</strong><small>Mark that this artist requires a deposit before tattoo work.</small></span>
                </label>

                {artistForm.requiresDeposit && (
                  <div className="form-group"><label>Deposit amount</label><input type="number" min="0.01" step="0.01" value={artistForm.depositAmount} onChange={(event) => setArtistField("depositAmount", event.target.value)} /></div>
                )}
              </div>
            </section>

            {setupMode === 0 ? (
              <section className="card form-card onboarding-section studio-information-section">
                <div className="onboarding-section-head">
                  <div><p className="subtitle inline-subtitle">Create studio</p><h2>Studio information</h2><p className="muted">This belongs to the studio. Your artist description and phone remain personal.</p></div>
                  <span className="step-chip">2</span>
                </div>
                <div className="form-group"><label>Studio name</label><input value={studioForm.name} onChange={(event) => setStudioField("name", event.target.value)} maxLength={120} placeholder="InkRoute Studio" /></div>
                <div className="form-group"><label>Studio description</label><textarea value={studioForm.description} onChange={(event) => setStudioField("description", event.target.value)} maxLength={1500} placeholder="Describe the studio, atmosphere, specialties and what clients can expect." /></div>
                <div className="form-group"><label>Address</label><input value={studioForm.address} onChange={(event) => setStudioField("address", event.target.value)} maxLength={220} placeholder="ul. Ivan Vazov 10" /></div>
                <div className="form-row">
                  <div className="form-group"><label>City</label><input value={studioForm.city} onChange={(event) => setStudioField("city", event.target.value)} maxLength={100} placeholder="Plovdiv" /></div>
                  <div className="form-group"><label>Country</label><input value={studioForm.country} onChange={(event) => setStudioField("country", event.target.value)} maxLength={100} /></div>
                </div>
              </section>
            ) : (
              <section className="card form-card onboarding-section studio-join-section">
                <div className="onboarding-section-head">
                  <div><p className="subtitle inline-subtitle">Join studio</p><h2>Find your studio</h2><p className="muted">Only studios currently accepting artists appear here.</p></div>
                  <span className="step-chip">2</span>
                </div>
                <div className="studio-search-box">
                  <input value={joinQuery} onChange={(event) => setJoinQuery(event.target.value)} placeholder="Type studio name, city or country..." onKeyDown={(event) => { if (event.key === "Enter") { event.preventDefault(); handleStudioSearch(); } }} />
                  <button className="secondary-button" type="button" onClick={handleStudioSearch} disabled={searchingStudios}>{searchingStudios ? "Searching..." : "Search"}</button>
                </div>
                <div className="join-studio-results">
                  {joinResults.map((studio) => (
                    <button type="button" key={studio.id} className={`join-studio-result ${selectedStudio?.id === studio.id ? "selected" : ""}`} onClick={() => setSelectedStudio(studio)}>
                      <div><strong>{studio.name}</strong><span>{studio.city}, {studio.country}</span><small>{studio.address}</small></div>
                      <div className="join-studio-meta"><span>{studio.artistCount} artist{studio.artistCount === 1 ? "" : "s"}</span><b>{selectedStudio?.id === studio.id ? "Selected" : "Select"}</b></div>
                    </button>
                  ))}
                  {!searchingStudios && joinQuery.trim().length >= 2 && joinResults.length === 0 && <p className="muted">No open studios found for this search.</p>}
                </div>
                {selectedStudio && <div className="selected-studio-banner"><strong>Join request will be sent to {selectedStudio.name}</strong><span>The owner must approve your request before your profile becomes part of the studio.</span></div>}
              </section>
            )}

            <section className="card form-card onboarding-section">
              <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Client rules</p><h2>Requirements</h2><p className="muted">Add only the rules clients should know before booking you.</p></div><span className="step-chip">3</span></div>
              {artistForm.requirements.map((requirement, index) => (
                <div className="inline-edit-row" key={index}>
                  <input value={requirement} onChange={(event) => updateRequirement(index, event.target.value)} placeholder={`Requirement ${index + 1}`} />
                  {artistForm.requirements.length > 1 && <button type="button" className="icon-danger-button" onClick={() => removeRequirement(index)}>×</button>}
                </div>
              ))}
              <button type="button" className="secondary-button" onClick={addRequirement}>Add requirement</button>
            </section>

            <section className="card form-card onboarding-section">
              <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Work</p><h2>Portfolio images</h2><p className="muted">Add several images now. New selections are appended instead of replacing the previous ones.</p></div><span className="step-chip">4</span></div>

              <label className="portfolio-upload-tile portfolio-upload-tile-create">
                <input
                  type="file"
                  accept="image/*"
                  multiple
                  hidden
                  onChange={(event) => {
                    addPortfolioFiles(event.target.files);
                    event.target.value = "";
                  }}
                />
                <span className="portfolio-upload-plus">＋</span>
                <strong>{portfolioImageFiles.length > 0 ? "Add more portfolio photos" : "Select portfolio photos"}</strong>
                <small>You can select multiple files at once or add more later.</small>
              </label>

              {portfolioPreviews.length > 0 && (
                <div className="portfolio-preview-grid portfolio-preview-grid-create">
                  {portfolioPreviews.map((preview, index) => (
                    <div className="portfolio-preview-card" key={`${preview.file.name}-${preview.file.size}-${preview.file.lastModified}`}>
                      <img src={preview.url} alt={`Portfolio preview ${index + 1}`} />
                      <div className="portfolio-preview-overlay"><span>{index + 1}</span><button type="button" onClick={() => removePortfolioFile(index)}>Remove</button></div>
                    </div>
                  ))}
                </div>
              )}
            </section>

            <section className="card form-card onboarding-section schedule-builder-section">
              <div className="onboarding-section-head"><div><p className="subtitle inline-subtitle">Availability</p><h2>Set your weekly schedule</h2><p className="muted">Pick days once, set the hours, done. Add another time block only when some days use different hours.</p></div><span className="step-chip">5</span></div>

              <WeeklyScheduleBuilder value={availability} onChange={setAvailability} />
            </section>

            {error && <p className="error artist-create-message">{error}</p>}
            {success && <p className="success artist-create-message">{success}</p>}

            <div className="artist-onboarding-submit">
              <div>
                <strong>{setupMode === 0 ? "Create artist + studio" : "Create artist + request to join"}</strong>
                <span>{setupMode === 0 ? "You will become the studio owner." : selectedStudio ? `Request: ${selectedStudio.name}` : "Choose a studio above."}</span>
              </div>
              <button className="primary-button" type="submit" disabled={submitting}>{submitting ? "Creating..." : "Create Artist Profile"}</button>
            </div>
          </form>
        )}
      </section>
    </main>
  );
}

export default CreateArtistProfilePage;
