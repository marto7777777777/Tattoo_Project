import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createArtistReview } from "../api/artistReviewApi";
import { readResponse } from "../api/http";

function CreateArtistReviewPage() {
  const navigate = useNavigate();
  const { tattooRequestId } = useParams();
  const [rating, setRating] = useState("5");
  const [comment, setComment] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    try {
      const response = await createArtistReview({
        tattooRequestId: Number(tattooRequestId),
        rating: Number(rating),
        comment: comment.trim() || null,
      });

      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccess("Review created successfully.");
      setTimeout(() => navigate("/bookings"), 800);
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header">
          <p className="subtitle">Artist Review</p>
          <h1>Rate your completed tattoo</h1>
          <p>Your public review shows only rating, comment, and date.</p>
        </div>

        <form className="form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Rating</label>
            <select value={rating} onChange={(event) => setRating(event.target.value)}>
              <option value="5">5 stars</option>
              <option value="4">4 stars</option>
              <option value="3">3 stars</option>
              <option value="2">2 stars</option>
              <option value="1">1 star</option>
            </select>
          </div>

          <div className="form-group">
            <label>Comment optional</label>
            <textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Great work, clean studio, professional artist..." />
          </div>

          {error && <p className="error">{error}</p>}
          {success && <p className="success">{success}</p>}

          <button className="primary-button">Submit Review</button>
        </form>
      </section>
    </main>
  );
}

export default CreateArtistReviewPage;
