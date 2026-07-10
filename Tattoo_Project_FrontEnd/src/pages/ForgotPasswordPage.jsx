import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { resetForgottenPassword, sendForgotPasswordCode, verifyForgotPasswordCode } from "../api/authApi";

function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSendCode(event) {
    event.preventDefault();
    setError(""); setSuccess(""); setIsSubmitting(true);

    try {
      await sendForgotPasswordCode(email);
      setSuccess("We sent a 6-digit code to your email. It expires in 10 minutes. If you do not see it, check your Spam folder.");
      setStep("code");
    } catch (err) {
      setError(err.message || "Password reset code could not be sent.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleVerifyCode(event) {
    event.preventDefault();
    setError(""); setSuccess(""); setIsSubmitting(true);

    try {
      await verifyForgotPasswordCode(email, code);
      setSuccess("Code verified. Enter your new password.");
      setStep("password");
    } catch (err) {
      setError(err.message || "Invalid or expired code.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResetPassword(event) {
    event.preventDefault();
    setError(""); setSuccess(""); setIsSubmitting(true);

    try {
      await resetForgottenPassword(email, code, newPassword, confirmNewPassword);
      setSuccess("Password changed successfully. Redirecting to login...");
      setTimeout(() => navigate("/login"), 3000);
    } catch (err) {
      setError(err.message || "Password could not be changed.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="center-container">
      <section className="card form-card">
        <div className="header">
          <p className="subtitle">Account recovery</p>
          <h1>Forgot password</h1>
          <p>
            {step === "email" && "Enter your email and we will send you a 6-digit code."}
            {step === "code" && `Enter the code sent to ${email}. If you do not see it, check your Spam folder.`}
            {step === "password" && "Choose your new password."}
          </p>
        </div>

        {step === "email" && (
          <form className="form" onSubmit={handleSendCode}>
            <div className="form-group"><label>Email</label><input type="email" value={email} onChange={(event) => setEmail(event.target.value)} /></div>
            {error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}
            <button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Sending..." : "Send code"}</button>
          </form>
        )}

        {step === "code" && (
          <form className="form" onSubmit={handleVerifyCode}>
            <div className="form-group">
              <label>Verification code</label>
              <input value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, "").slice(0, 6))} inputMode="numeric" maxLength="6" placeholder="123456" />
            </div>
            {error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}
            <button className="primary-button" type="submit" disabled={isSubmitting || code.length !== 6}>{isSubmitting ? "Verifying..." : "Verify code"}</button>
            <button className="secondary-button" type="button" onClick={() => setStep("email")}>Change email</button>
          </form>
        )}

        {step === "password" && (
          <form className="form" onSubmit={handleResetPassword}>
            <div className="form-group"><label>New password</label><input type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} /></div>
            <div className="form-group"><label>Confirm new password</label><input type="password" value={confirmNewPassword} onChange={(event) => setConfirmNewPassword(event.target.value)} /></div>
            {error && <p className="error">{error}</p>}{success && <p className="success">{success}</p>}
            <button className="primary-button" type="submit" disabled={isSubmitting}>{isSubmitting ? "Saving..." : "Change password"}</button>
          </form>
        )}

        <p className="muted footer-link"><Link to="/login">Back to login</Link></p>
      </section>
    </main>
  );
}

export default ForgotPasswordPage;
