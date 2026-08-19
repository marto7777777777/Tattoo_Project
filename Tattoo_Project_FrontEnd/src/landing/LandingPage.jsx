import { useEffect, useMemo, useRef } from "react";
import { useSearchParams } from "react-router-dom";
import { useLanguage } from "../i18n/LanguageContext";
import LandingHeader from "./LandingHeader";
import LandingMedia from "./LandingMedia";
import { landingCopy } from "./landingContent";
import { landingV2Copy } from "./landingV2Content";
import { getLandingFaq } from "./landingFaq";
import { getDeviceCta, landingVideos } from "./landingConfig";
import { trackLanding } from "./landingTracking";
import { useLandingSeo } from "./useLandingSeo";
import "./landing.css";

const Icon = ({ children }) => <span className="landing-icon" aria-hidden="true">{children}</span>;
const SectionHead = ({ eyebrow, title, body }) => <div className="landing-section-head"><span>{eyebrow}</span><h2>{title}</h2>{body && <p>{body}</p>}</div>;

export default function LandingPage() {
  const { language } = useLanguage();
  const copy = landingCopy[language] || landingCopy.en;
  const detail = landingV2Copy[language] || landingV2Copy.en;
  const [params] = useSearchParams();
  const hook = params.get("hook");
  const hookTitle = { time: copy.hookTime, messages: copy.hookMessages, unclear: copy.hookUnclear }[hook];
  const cta = useMemo(() => { const value = getDeviceCta(); return { url: value.href, kind: value.store }; }, []);
  const trackedDepth = useRef(new Set());
  const hasOverviewVideo = Boolean(landingVideos.overview.src);
  const faq = getLandingFaq(language);
  useLandingSeo();

  useEffect(() => {
    trackLanding("page_view", { language, hook: hookTitle ? hook : "default" });
    const onScroll = () => {
      const total = document.documentElement.scrollHeight - window.innerHeight;
      if (total <= 0) return;
      const depth = Math.round((window.scrollY / total) * 100);
      [25, 50, 75, 100].forEach((mark) => { if (depth >= mark && !trackedDepth.current.has(mark)) { trackedDepth.current.add(mark); trackLanding("scroll_depth", { depth: mark }); } });
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [hook, hookTitle, language]);

  return <div id="top" className="artist-landing" data-i18n-ignore>
    <LandingHeader copy={copy} cta={cta} />
    <main>
      <section className={`landing-hero landing-shell ${hasOverviewVideo ? "" : "landing-hero-no-media"}`}>
        <div className="landing-hero-copy"><span className="landing-eyebrow">{copy.eyebrow}</span><h1>{hookTitle || copy.heroTitle}</h1><p>{copy.heroBody}</p><p className="landing-hero-detail">{detail.heroDetail}</p><div className="landing-cta-row"><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "hero", destination: cta.kind })}>{copy.start}</a><a className="landing-button landing-button-ghost" href="#how-it-works">{copy.seeHow}</a></div><small>{copy.offerNote}</small></div>
        {hasOverviewVideo && <LandingMedia video={landingVideos.overview} title={copy.mediaHero} priority />}
      </section>

      <section className="landing-section landing-shell landing-panel landing-setup" id="how-it-works">
        <SectionHead eyebrow={detail.setupEyebrow} title={detail.setupTitle} body={detail.setupIntro} />
        <ol className="landing-setup-steps">{detail.setupSteps.map((step, index) => <li key={step}><span>{String(index + 1).padStart(2, "0")}</span><strong>{step}</strong></li>)}</ol>
        <div className="landing-profile-summary" id="profile"><div><Icon>IR</Icon><h3>{detail.profileTitle}</h3><p>{detail.profileBody}</p></div><div className="landing-profile-rules"><p>{detail.profilePrivacy}</p><p>{detail.profileLink}</p></div></div>
        <aside className="landing-tip"><strong>{copy.proTip}</strong><p>{copy.proTipBody}</p></aside>
      </section>

      <section className="landing-section landing-shell" id="schedule">
        <SectionHead eyebrow={copy.scheduleEyebrow} title={copy.scheduleTitle} body={copy.scheduleBody} />
        <div className="landing-schedule-layout"><article className="landing-schedule-example"><h3>{detail.scheduleExampleTitle}</h3>{detail.scheduleExample.map(([days, time, type]) => <div key={`${days}-${time}`}><strong>{days}</strong><span>{time}</span><em>{type}</em></div>)}</article><div className="landing-schedule-lists"><ul>{detail.scheduleControls.map((item) => <li key={item}><Icon>✓</Icon><span>{item}</span></li>)}</ul><div><h3>{detail.scheduleExcludesTitle}</h3><ul>{detail.scheduleExcludes.map((item) => <li key={item}><Icon>−</Icon><span>{item}</span></li>)}</ul></div></div></div>
        <blockquote>{copy.scheduleStatement}</blockquote>
        <div className="landing-featured-video"><LandingMedia video={landingVideos.featured.artistSetup} title={`${copy.videoProfile} · ${copy.videoWeeklySchedule}`} description={copy.videoComingSoon} showPlaceholder /></div>
      </section>

      <section className="landing-section landing-shell landing-workflow" id="workflow">
        <SectionHead eyebrow={detail.workflowEyebrow} title={detail.workflowTitle} body={detail.workflowBody} />
        <ol className="landing-workflow-timeline">{detail.timeline.map((step, index) => <li key={step.title} className={index === 2 ? "landing-workflow-decision" : ""}><span className="landing-step-number">{String(index + 1).padStart(2, "0")}</span><div><h3>{step.title}</h3><p>{step.body}</p>{step.items && <ul>{step.items.map((item) => <li key={item}>{item}</li>)}</ul>}</div>{index === 2 && <div className="landing-workflow-path-wrap"><h4>{detail.pathsTitle}</h4><div className="landing-paths"><article><Icon>A</Icon><h3>{detail.consultationPathTitle}</h3><p>{detail.consultationPathBody}</p></article><article><Icon>B</Icon><h3>{detail.directPathTitle}</h3><p>{detail.directPathBody}</p></article></div></div>}</li>)}</ol>
        <blockquote>{detail.bookingNote}</blockquote>
        <div className="landing-featured-video"><LandingMedia video={landingVideos.featured.completeWorkflow} title={copy.videoOverview} description={copy.videoComingSoon} showPlaceholder /></div>
      </section>

      <section className="landing-section landing-shell landing-offer"><div><SectionHead eyebrow={copy.offerEyebrow} title={copy.offerTitle} /><ul>{copy.offerItems.map((item) => <li key={item}>✓ {item}</li>)}</ul><small>{copy.offerDisclaimer}</small></div><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "offer", destination: cta.kind })}>{copy.start}</a></section>
      <section className="landing-section landing-shell" id="faq"><SectionHead eyebrow={copy.faqEyebrow} title={copy.faqTitle} /><div className="landing-faq">{faq.map(([question, answer], index) => <details key={question} onToggle={(event) => event.currentTarget.open && trackLanding("faq_open", { index })}><summary>{question}<span>+</span></summary><p>{answer}</p></details>)}</div></section>
      <section className="landing-final"><div className="landing-shell"><h2>{copy.finalTitle}</h2><p>{copy.finalBody}</p><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "final", destination: cta.kind })}>{copy.start}</a></div></section>
    </main>
    <footer className="landing-footer landing-shell"><div className="landing-brand"><img src="/inkroute-app-icon.png" alt="" width="42" height="42" /><strong>InkRoute</strong></div><p>{copy.footerBody}</p><small>© {new Date().getFullYear()} InkRoute</small></footer>
  </div>;
}
