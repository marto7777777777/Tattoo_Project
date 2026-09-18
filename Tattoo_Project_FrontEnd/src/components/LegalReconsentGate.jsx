import { useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { acceptCurrentLegalVersions, getLegalConsentStatus } from "../api/authApi";
import { useAuth } from "../context/AuthContext";

const EXEMPT_PATHS = ["/login", "/register", "/forgot-password", "/legal/privacy", "/legal/terms"];

export default function LegalReconsentGate() {
  const { isLoggedIn } = useAuth();
  const { pathname } = useLocation();
  const [required, setRequired] = useState(false);
  const [accepted, setAccepted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;
    if (!isLoggedIn || EXEMPT_PATHS.includes(pathname)) return undefined;
    getLegalConsentStatus().then((status) => {
      if (!cancelled) setRequired(Boolean(status?.requiresReconsent));
    }).catch((err) => { if (!cancelled) setError(err.message || "Legal consent status could not be loaded."); });
    return () => { cancelled = true; };
  }, [isLoggedIn, pathname]);

  if (!isLoggedIn || !required || EXEMPT_PATHS.includes(pathname)) return null;

  async function confirm() {
    if (!accepted || busy) return;
    setBusy(true); setError("");
    try { await acceptCurrentLegalVersions(); setRequired(false); }
    catch (err) { setError(err.message || "The current legal documents could not be accepted."); }
    finally { setBusy(false); }
  }

  return <div className="modal-backdrop studio-confirm-backdrop legal-reconsent-backdrop">
    <section className="modal-card studio-confirm-modal" role="dialog" aria-modal="true" aria-labelledby="legal-reconsent-title">
      <p className="subtitle inline-subtitle">Updated legal documents</p>
      <h2 id="legal-reconsent-title">Review and accept the current terms</h2>
      <p>To continue using InkRoute, review the current <Link to="/legal/terms" target="_blank">Terms of Service</Link> and <Link to="/legal/privacy" target="_blank">Privacy Policy</Link>.</p>
      <label className="legal-acceptance"><input type="checkbox" checked={accepted} onChange={(event) => setAccepted(event.target.checked)} /><span>I accept the current Terms of Service and Privacy Policy.</span></label>
      {error && <p className="error">{error}</p>}
      <button className="primary-button" type="button" disabled={!accepted || busy} onClick={confirm}>{busy ? "Saving..." : "Accept and continue"}</button>
    </section>
  </div>;
}
