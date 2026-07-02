import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { loginUser } from "../api/authApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

function LoginPage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [form, setForm] = useState({ login: "", password: "" });
  const [error, setError] = useState("");

  function handleChange(event) { setForm({ ...form, [event.target.name]: event.target.value }); }

  async function handleSubmit(event) {
    event.preventDefault(); setError("");
    try {
      const response = await loginUser(form);
      const data = await readResponse(response);
      if (!response.ok) { setError(typeof data === "string" ? data : "Invalid login credentials."); return; }
      saveAuthToken(data.token);
      const roles = data.user?.roles || data.user?.Roles || [];
      if (roles.length === 0) navigate("/choose-profile");
      else navigate("/artists");
    } catch { setError("Server connection failed. Please try again."); }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header"><p className="subtitle">Welcome back</p><h1>Login</h1><p>Log in to manage your tattoo workflow.</p></div>
        <form className="form" onSubmit={handleSubmit}>
          <div className="form-group"><label>Email or username</label><input name="login" value={form.login} onChange={handleChange} /></div>
          <div className="form-group"><label>Password</label><input name="password" type="password" value={form.password} onChange={handleChange} /></div>
          {error && <p className="error">{error}</p>}
          <button className="primary-button" type="submit">Log in</button>
        </form>
        <p className="muted footer-link">No account? <Link to="/register">Register</Link></p>
      </section>
    </main>
  );
}
export default LoginPage;
