import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { registerUser } from "../api/authApi";
import { readResponse } from "../api/http";

function RegisterPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({ firstName: "", lastName: "", userName: "", email: "", password: "" });
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleChange(event) { setForm({ ...form, [event.target.name]: event.target.value }); }

  async function handleSubmit(event) {
    event.preventDefault();
    setError(""); setSuccessMessage("");
    try {
      const response = await registerUser(form);
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : JSON.stringify(data)); return; }
      setSuccessMessage("Registration successful. Redirecting to login...");
      setTimeout(() => navigate("/login"), 800);
    } catch { setError("Server connection failed. Please try again."); }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header"><p className="subtitle">Create account</p><h1>Join the platform</h1><p>Register to create a client or artist profile.</p></div>
        <form className="form" onSubmit={handleSubmit}>
          <div className="form-row"><div className="form-group"><label>First name</label><input name="firstName" value={form.firstName} onChange={handleChange} /></div><div className="form-group"><label>Last name</label><input name="lastName" value={form.lastName} onChange={handleChange} /></div></div>
          <div className="form-group"><label>Username</label><input name="userName" value={form.userName} onChange={handleChange} /></div>
          <div className="form-group"><label>Email</label><input name="email" type="email" value={form.email} onChange={handleChange} /></div>
          <div className="form-group"><label>Password</label><input name="password" type="password" value={form.password} onChange={handleChange} /></div>
          {error && <p className="error">{error}</p>}{successMessage && <p className="success">{successMessage}</p>}
          <button className="primary-button" type="submit">Register</button>
        </form>
        <p className="muted footer-link">Already have an account? <Link to="/login">Log in</Link></p>
      </section>
    </main>
  );
}
export default RegisterPage;
