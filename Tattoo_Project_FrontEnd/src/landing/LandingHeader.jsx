import { useEffect, useState } from "react";
import LanguageSwitcher from "../components/LanguageSwitcher";
import { trackLanding } from "./landingTracking";

export default function LandingHeader({ copy, cta }) {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    const close = (event) => event.key === "Escape" && setOpen(false);
    document.addEventListener("keydown", close);
    return () => document.removeEventListener("keydown", close);
  }, []);
  const links = [["how-it-works", copy.navHow], ["workflow", copy.navWorkflow], ["schedule", copy.navSchedule], ["profile", copy.navProfile], ["faq", copy.navFaq]];
  return <header className="landing-header">
    <a href="#top" className="landing-brand" aria-label="InkRoute"><img src="/inkroute-app-icon.png" alt="" width="42" height="42" /><strong>InkRoute</strong></a>
    <button className="landing-menu-button" type="button" aria-expanded={open} aria-controls="landing-navigation" onClick={() => setOpen(!open)}><span /><span /><span /><span className="sr-only">Menu</span></button>
    <nav id="landing-navigation" className={open ? "is-open" : ""} aria-label="Landing navigation">
      {links.map(([id, label]) => <a key={id} href={`#${id}`} onClick={() => setOpen(false)}>{label}</a>)}
    </nav>
    <div className="landing-header-actions"><LanguageSwitcher className="landing-language" /><a className="landing-button landing-button-small" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "header", destination: cta.kind })}>{copy.start}</a></div>
  </header>;
}
