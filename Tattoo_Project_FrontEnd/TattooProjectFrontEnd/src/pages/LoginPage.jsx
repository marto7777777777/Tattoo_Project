
import { useState } from "react";
import { loginUser } from "../api/authApi";
import "../styles/auth.css";

function LoginPage() {
  const [form, setForm] = useState({
    login: "",
    password: "",
  });

  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleChange(event) {
    setForm({
      ...form,
      [event.target.name]: event.target.value,
    });
  }

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");
    setSuccessMessage("");

    try {
      const response = await loginUser(form);

      if (!response.ok) {
        const errorMessage = await response.text();
        setError(errorMessage || "Invalid login credentials.");
        return;
      }

      const data = await response.json();

      localStorage.setItem("token", data.token);

      setSuccessMessage("Login successful.");
      setForm({
        login: "",
        password: "",
      });
    } catch {
      setError("Server connection failed. Please try again.");
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-card">
        <div className="auth-header">
          <p className="auth-subtitle">Tattoo Booking Platform</p>
          <h1>Welcome Back</h1>
          <p className="auth-description">
            Log in to manage tattoo requests, consultations, and sessions.
          </p>
        </div>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="login">Email or username</label>
            <input
              id="login"
              name="login"
              type="text"
              placeholder="Enter your email or username"
              value={form.login}
              onChange={handleChange}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              placeholder="Enter your password"
              value={form.password}
              onChange={handleChange}
            />
          </div>

          {error && <p className="auth-error">{error}</p>}
          {successMessage && <p className="auth-success">{successMessage}</p>}

          <button type="submit" className="auth-button">
            Log In
          </button>
        </form>

        <p className="auth-footer">
          Don&apos;t have an account? <span>Register</span>
        </p>
      </section>
    </main>
  );
}

export default LoginPage;