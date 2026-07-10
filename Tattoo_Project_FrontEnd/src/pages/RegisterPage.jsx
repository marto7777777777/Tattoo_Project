import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { registerUser, resendRegisterCode, verifyRegisterCode } from "../api/authApi";
import { readResponse } from "../api/http";
import { useAuth } from "../context/AuthContext";

function RegisterPage() {
  const navigate = useNavigate();
  const { saveAuthToken } = useAuth();
  const [form, setForm] = useState({ firstName: "", lastName: "", userName: "", email: "", password: "" });
  const [code, setCode] = useState("");
  const [step, setStep] = useState("register");
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleChange(event) { setForm({ ...form, [event.target.name]: event.target.value }); }

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setSuccessMessage("");
    setIsSubmitting(true);

    try {
      const response = await registerUser(form);
      const data = await readResponse(response);

      if (!response.ok) {
        setError(typeof data === "string" ? data : JSON.stringify(data));
        return;
      }

      setSuccessMessage("We sent a 6-digit code to your email. It expires in 10 minutes. If you do not see it, check your Spam folder.");
      setStep("verify");
    } catch {
      setError("Server connection failed. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleVerifyCode(event) {
    event.preventDefault();
    setError("");
    setSuccessMessage("");
    setIsSubmitting(true);

    try {
      const data = await verifyRegisterCode(form.email, code);
      saveAuthToken(data.token);
      setSuccessMessage("Email verified successfully. Redirecting...");
      setTimeout(() => navigate("/choose-profile"), 3000);
    } catch (err) {
      setError(err.message || "Invalid or expired verification code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResendCode() {
    setError("");
    setSuccessMessage("");

    try {
      await resendRegisterCode(form.email);
      setSuccessMessage("New verification code sent successfully. If you do not see it, check your Spam folder.");
    } catch (err) {
      setError(err.message || "Verification code could not be sent.");
    }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header">
          <p className="subtitle">Create account</p>
          <h1>{step === "register" ? "Join the platform" : "Check your email"}</h1>
          <p>
            {step === "register"
              ? "Register to create a client or artist profile."
              : `We sent a 6-digit code to ${form.email}. It expires in 10 minutes. If you do not see it, check your Spam folder.`}
          </p>
        </div>

        {step === "register" ? (
          <form className="form" onSubmit={handleSubmit}>
            <div className="form-row">
              <div className="form-group"><label>First name</label><input name="firstName" value={form.firstName} onChange={handleChange} /></div>
              <div className="form-group"><label>Last name</label><input name="lastName" value={form.lastName} onChange={handleChange} /></div>
            </div>
            <div className="form-group"><label>Username</label><input name="userName" value={form.userName} onChange={handleChange} /></div>
            <div className="form-group"><label>Email</label><input name="email" type="email" value={form.email} onChange={handleChange} /></div>
            <div className="form-group"><label>Password</label><input name="password" type="password" value={form.password} onChange={handleChange} /></div>
            {error && <p className="error">{error}</p>}{successMessage && <p className="success">{successMessage}</p>}
            <button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Sending code..." : "Register"}</button>
          </form>
        ) : (
          <form className="form" onSubmit={handleVerifyCode}>
            <div className="form-group">
              <label>Verification code</label>
              <input
                value={code}
                onChange={(event) => setCode(event.target.value.replace(/\D/g, "").slice(0, 6))}
                inputMode="numeric"
                maxLength="6"
                placeholder="123456"
              />
            </div>
            {error && <p className="error">{error}</p>}{successMessage && <p className="success">{successMessage}</p>}
            <button className="primary-button" type="submit" disabled={isSubmitting || code.length !== 6}>{isSubmitting ? "Verifying..." : "Verify email"}</button>
            <button className="secondary-button" type="button" onClick={handleResendCode}>Resend code</button>
          </form>
        )}

        <p className="muted footer-link">Already have an account? <Link to="/login">Log in</Link></p>
      </section>
    </main>
  );
}
export default RegisterPage;
