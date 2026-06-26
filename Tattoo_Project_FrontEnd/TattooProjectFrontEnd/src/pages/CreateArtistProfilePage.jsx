import { useState } from "react";
import { createArtistProfile } from "../api/artistApi";
import "../styles/profileForm.css";

function CreateArtistProfilePage() {
  const [form, setForm] = useState({
    studioName: "",
    description: "",
    studioAddress: "",
    phoneNumber: "",
    offersOnlineConsultation: false,
    requiresDeposit: false,
    depositAmount: "",
    consultationDurationMinutes: "",
    requirements: [""],
    portfolioImages: [""],
    schedules: [
      {
        dayOfWeek: "",
        startTime: "",
        endTime: "",
        scheduleType: "",
      },
    ],
  });

  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleChange(event) {
    const { name, value, type, checked } = event.target;

    setForm({
      ...form,
      [name]: type === "checkbox" ? checked : value,
    });
  }

  function handleRequirementChange(index, value) {
    const updatedRequirements = [...form.requirements];
    updatedRequirements[index] = value;

    setForm({
      ...form,
      requirements: updatedRequirements,
    });
  }

  function addRequirement() {
    setForm({
      ...form,
      requirements: [...form.requirements, ""],
    });
  }

  function handlePortfolioImageChange(index, value) {
    const updatedPortfolioImages = [...form.portfolioImages];
    updatedPortfolioImages[index] = value;

    setForm({
      ...form,
      portfolioImages: updatedPortfolioImages,
    });
  }

  function addPortfolioImage() {
    setForm({
      ...form,
      portfolioImages: [...form.portfolioImages, ""],
    });
  }

  function handleScheduleChange(index, field, value) {
    const updatedSchedules = [...form.schedules];

    updatedSchedules[index] = {
      ...updatedSchedules[index],
      [field]: value,
    };

    setForm({
      ...form,
      schedules: updatedSchedules,
    });
  }

  function addSchedule() {
    setForm({
      ...form,
      schedules: [
        ...form.schedules,
        {
          dayOfWeek: "",
          startTime: "",
          endTime: "",
          scheduleType: "",
        },
      ],
    });
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setSuccessMessage("");

    const schedules = form.schedules
      .filter(
        (schedule) =>
          schedule.dayOfWeek !== "" &&
          schedule.startTime !== "" &&
          schedule.endTime !== "" &&
          schedule.scheduleType !== ""
      )
      .map((schedule) => ({
        dayOfWeek: Number(schedule.dayOfWeek),
        startTime: `${schedule.startTime}:00`,
        endTime: `${schedule.endTime}:00`,
        scheduleType: Number(schedule.scheduleType),
      }));

    const artistData = {
      studioName: form.studioName,
      description: form.description,
      studioAddress: form.studioAddress,
      phoneNumber: form.phoneNumber,
      offersOnlineConsultation: form.offersOnlineConsultation,
      requiresDeposit: form.requiresDeposit,
      depositAmount: form.requiresDeposit ? Number(form.depositAmount) : null,
      consultationDurationMinutes: Number(form.consultationDurationMinutes),

      requirements: form.requirements
        .filter((requirement) => requirement.trim() !== "")
        .map((requirement) => ({
          description: requirement,
        })),

      portfolioImages: form.portfolioImages
        .filter((imageUrl) => imageUrl.trim() !== "")
        .map((imageUrl) => ({
          imageUrl: imageUrl,
        })),

      schedules: schedules,
    };

    try {
      const response = await createArtistProfile(artistData);

      if (!response.ok) {
        const errorText = await response.text();
        setError(errorText || "Failed to create tattoo artist profile.");
        return;
      }

      const data = await response.json();

      if (data.token) {
        localStorage.setItem("token", data.token);
      }

      setSuccessMessage("Tattoo artist profile created successfully.");

      setForm({
        studioName: "",
        description: "",
        studioAddress: "",
        phoneNumber: "",
        offersOnlineConsultation: false,
        requiresDeposit: false,
        depositAmount: "",
        consultationDurationMinutes: "",
        requirements: [""],
        portfolioImages: [""],
        schedules: [
          {
            dayOfWeek: "",
            startTime: "",
            endTime: "",
            scheduleType: "",
          },
        ],
      });
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="profile-form-page">
      <section className="profile-form-card profile-form-card-large">
        <div className="profile-form-header">
          <p className="profile-form-subtitle">Tattoo Artist Profile</p>
          <h1>Create your artist profile</h1>
          <p>
            Add your studio information, consultation settings, portfolio,
            requirements, and working schedule.
          </p>
        </div>

        <form className="profile-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="studioName">Studio name</label>
            <input
              id="studioName"
              name="studioName"
              type="text"
              placeholder="Enter your studio name"
              value={form.studioName}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="description">Description</label>
            <textarea
              id="description"
              name="description"
              placeholder="Describe your tattoo style, experience, and studio"
              value={form.description}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="studioAddress">Studio address</label>
            <input
              id="studioAddress"
              name="studioAddress"
              type="text"
              placeholder="Enter your studio address"
              value={form.studioAddress}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="phoneNumber">Phone number</label>
            <input
              id="phoneNumber"
              name="phoneNumber"
              type="text"
              placeholder="Enter your phone number"
              value={form.phoneNumber}
              onChange={handleChange}
            />
          </div>

          <div className="profile-section">
            <h2>Consultation settings</h2>

            <div className="form-group">
              <label htmlFor="consultationDurationMinutes">
                Consultation duration in minutes
              </label>
              <input
                id="consultationDurationMinutes"
                name="consultationDurationMinutes"
                type="number"
                min="15"
                max="180"
                placeholder="Example: 30"
                value={form.consultationDurationMinutes}
                onChange={handleChange}
              />
            </div>

            <div className="checkbox-row">
              <label>
                <input
                  name="offersOnlineConsultation"
                  type="checkbox"
                  checked={form.offersOnlineConsultation}
                  onChange={handleChange}
                />
                Offers online consultation
              </label>

              <label>
                <input
                  name="requiresDeposit"
                  type="checkbox"
                  checked={form.requiresDeposit}
                  onChange={handleChange}
                />
                Requires deposit
              </label>
            </div>

            {form.requiresDeposit && (
              <div className="form-group">
                <label htmlFor="depositAmount">Deposit amount</label>
                <input
                  id="depositAmount"
                  name="depositAmount"
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="Example: 50"
                  value={form.depositAmount}
                  onChange={handleChange}
                />
              </div>
            )}
          </div>

          <div className="profile-section">
            <h2>Requirements</h2>

            {form.requirements.map((requirement, index) => (
              <div className="form-group" key={index}>
                <label>Requirement {index + 1}</label>
                <input
                  type="text"
                  placeholder="Example: Send clear reference images"
                  value={requirement}
                  onChange={(event) =>
                    handleRequirementChange(index, event.target.value)
                  }
                />
              </div>
            ))}

            <button
              type="button"
              className="secondary-button"
              onClick={addRequirement}
            >
              Add Requirement
            </button>
          </div>

          <div className="profile-section">
            <h2>Portfolio images</h2>

            {form.portfolioImages.map((imageUrl, index) => (
              <div className="form-group" key={index}>
                <label>Image URL {index + 1}</label>
                <input
                  type="text"
                  placeholder="Paste portfolio image URL"
                  value={imageUrl}
                  onChange={(event) =>
                    handlePortfolioImageChange(index, event.target.value)
                  }
                />
              </div>
            ))}

            <button
              type="button"
              className="secondary-button"
              onClick={addPortfolioImage}
            >
              Add Portfolio Image
            </button>
          </div>

          <div className="profile-section">
            <h2>Schedule</h2>
            <p className="section-helper-text">
              Add separate schedule blocks for consultations and tattoo
              sessions.
            </p>

            {form.schedules.map((schedule, index) => (
              <div className="schedule-row schedule-row-with-type" key={index}>
                <div className="form-group">
                  <label>Day</label>
                  <select
                    value={schedule.dayOfWeek}
                    onChange={(event) =>
                      handleScheduleChange(index, "dayOfWeek", event.target.value)
                    }
                  >
                    <option value="">Select day</option>
                    <option value="1">Monday</option>
                    <option value="2">Tuesday</option>
                    <option value="3">Wednesday</option>
                    <option value="4">Thursday</option>
                    <option value="5">Friday</option>
                    <option value="6">Saturday</option>
                    <option value="0">Sunday</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>Start time</label>
                  <input
                    type="time"
                    value={schedule.startTime}
                    onChange={(event) =>
                      handleScheduleChange(index, "startTime", event.target.value)
                    }
                  />
                </div>

                <div className="form-group">
                  <label>End time</label>
                  <input
                    type="time"
                    value={schedule.endTime}
                    onChange={(event) =>
                      handleScheduleChange(index, "endTime", event.target.value)
                    }
                  />
                </div>

                <div className="form-group">
                  <label>Schedule type</label>
                  <select
                    value={schedule.scheduleType}
                    onChange={(event) =>
                      handleScheduleChange(
                        index,
                        "scheduleType",
                        event.target.value
                      )
                    }
                  >
                    <option value="">Select type</option>
                    <option value="1">Consultation</option>
                    <option value="0">Tattoo session</option>
                  </select>
                </div>
              </div>
            ))}

            <button
              type="button"
              className="secondary-button"
              onClick={addSchedule}
            >
              Add Schedule
            </button>
          </div>

          {error && <p className="profile-error">{error}</p>}
          {successMessage && <p className="profile-success">{successMessage}</p>}

          <button type="submit" className="profile-submit-button">
            Create Tattoo Artist Profile
          </button>
        </form>
      </section>
    </main>
  );
}

export default CreateArtistProfilePage;