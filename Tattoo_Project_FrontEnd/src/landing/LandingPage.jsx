import { useEffect, useMemo, useRef } from "react";
import { useSearchParams } from "react-router-dom";
import { useLanguage } from "../i18n/LanguageContext";
import LandingHeader from "./LandingHeader";
import LandingMedia from "./LandingMedia";
import { landingCopy } from "./landingContent";
import { landingV2Copy } from "./landingV2Content";
import { landingSalesCopy } from "./landingSalesContent";
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
  const sales = landingSalesCopy[language] || landingSalesCopy.en;
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
        <div className="landing-hero-copy"><span className="landing-eyebrow">{copy.eyebrow}</span><h1>{hookTitle || sales.heroTitle}</h1><p>{sales.heroBody}</p><p className="landing-hero-detail">{sales.heroDetail}</p><div className="landing-cta-row"><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "hero", destination: cta.kind })}>{copy.start}</a><a className="landing-button landing-button-ghost" href="#workflow" onClick={() => trackLanding("secondary_cta_click", { placement: "hero", destination: "workflow" })}>{copy.seeHow}</a></div><small>{copy.offerNote}</small></div>
        {hasOverviewVideo && <LandingMedia video={landingVideos.overview} title={copy.mediaHero} priority />}
      </section>

      <section className="landing-section landing-shell landing-outcomes" id="how-it-works">
        <SectionHead eyebrow={sales.outcomesEyebrow} title={sales.outcomesTitle} />
        <div className="landing-outcome-grid">{sales.outcomes.map(([title, body], index) => <article key={title}><Icon>{String(index + 1).padStart(2, "0")}</Icon><h3>{title}</h3><p>{body}</p></article>)}</div>
      </section>

      <section className="landing-section landing-shell landing-workflow" id="workflow">
        <SectionHead eyebrow={detail.workflowEyebrow} title={detail.workflowTitle} body={detail.workflowBody} />
        <ol className="landing-workflow-timeline">{detail.timeline.map((step, index) => <li key={step.title} className={index === 2 ? "landing-workflow-decision" : ""}><span className="landing-step-number">{String(index + 1).padStart(2, "0")}</span><div><h3>{step.title}</h3><p>{step.body}</p>{step.items && <ul>{step.items.map((item) => <li key={item}>{item}</li>)}</ul>}</div>{index === 2 && <div className="landing-workflow-path-wrap"><h4>{detail.pathsTitle}</h4><div className="landing-paths"><article><Icon>A</Icon><h3>{detail.consultationPathTitle}</h3><p>{detail.consultationPathBody}</p></article><article><Icon>B</Icon><h3>{detail.directPathTitle}</h3><p>{detail.directPathBody}</p></article></div></div>}</li>)}</ol>
        <blockquote>{detail.bookingNote}</blockquote>
        <div className="landing-featured-video"><LandingMedia video={landingVideos.featured.completeWorkflow} title={copy.videoOverview} description={copy.videoComingSoon} showPlaceholder /></div>
      </section>

      <section className="landing-section landing-shell landing-panel landing-setup" id="profile">
        <SectionHead eyebrow={detail.setupEyebrow} title={sales.setupTitle} body={sales.setupIntro} />
        <div className="landing-setup-columns">
          <article><Icon>IR</Icon><h3>{sales.setupProfileTitle}</h3><ul>{sales.profileItems.map((item) => <li key={item}>✓ <span>{item}</span></li>)}</ul><p>{sales.profileNote}</p></article>
          <article id="schedule"><Icon>◷</Icon><h3>{sales.scheduleCardTitle}</h3><ul>{sales.scheduleItems.map((item) => <li key={item}>✓ <span>{item}</span></li>)}</ul><p>{sales.scheduleNote}</p></article>
        </div>
        <aside className="landing-tip"><strong>{copy.proTip}</strong><p>{copy.proTipBody}</p></aside>
        <div className="landing-featured-video"><LandingMedia video={landingVideos.featured.artistSetup} title={`${copy.videoProfile} · ${copy.videoWeeklySchedule}`} description={copy.videoComingSoon} showPlaceholder /></div>
      </section>

      <section className="landing-section landing-shell landing-pricing" id="pricing">
        <div><span className="landing-eyebrow">{language === "bg" ? "Цени" : "Pricing"}</span><h2>€14.99 <small>/ {language === "bg" ? "месец" : "month"}</small></h2><h3>{language === "bg" ? "Първите 3 месеца безплатно" : "First 3 months free"}</h3><p>{language === "bg" ? "Напълно безплатно за клиентите." : "Completely free for clients."}</p></div>
        <ul><li>{language === "bg" ? "Карта се изисква при започване на trial периода." : "A card is required when the trial starts."}</li><li>{language === "bg" ? "Няма таксуване през първите 3 месеца от стартирането на абонамента." : "No charge during the first 3 months from the start of the subscription."}</li><li>{language === "bg" ? "След това — автоматично месечно подновяване." : "Automatic monthly renewal after the trial."}</li><li>{language === "bg" ? "Данъците се изчисляват и показват в Stripe Checkout." : "Applicable taxes are calculated and shown in Stripe Checkout."}</li><li>{language === "bg" ? "Можеш да прекратиш преди първото плащане." : "Cancel before the first payment."}</li></ul>
      </section>

      <section className="landing-section landing-shell landing-offer"><div><SectionHead eyebrow={sales.offerEyebrow} title={sales.offerTitle} /><small>{copy.offerDisclaimer}</small></div><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "offer", destination: cta.kind })}>{copy.start}</a></section>
      <section className="landing-section landing-shell" id="faq"><SectionHead eyebrow={copy.faqEyebrow} title={copy.faqTitle} /><div className="landing-faq">{faq.map(([question, answer], index) => <details key={question} onToggle={(event) => event.currentTarget.open && trackLanding("faq_open", { index })}><summary>{question}<span>+</span></summary><p>{answer}</p></details>)}</div></section>
      <section className="landing-final"><div className="landing-shell"><h2>{sales.finalTitle}</h2><p>{sales.finalBody}</p><a className="landing-button" href={cta.url} onClick={() => trackLanding("cta_click", { placement: "final", destination: cta.kind })}>{copy.start}</a></div></section>
    </main>
    <footer className="landing-footer landing-shell"><div className="landing-brand"><img src="/inkroute-app-icon.png" alt="" width="42" height="42" /><strong>InkRoute</strong></div><nav className="landing-footer-links"><a href="/legal/legal-notice">{language==="bg"?"Фирмена информация":"Legal Notice"}</a><a href="/legal/terms">{language==="bg"?"Общи условия":"Terms"}</a><a href="/legal/privacy">{language==="bg"?"Поверителност":"Privacy"}</a><a href="/legal/cookies">{language==="bg"?"Бисквитки":"Cookies"}</a><a href="/legal/subscription">{language==="bg"?"Абонамент":"Subscription"}</a><a href="/legal/support">{language==="bg"?"Контакти":"Support"}</a><a href="/account-deletion">{language==="bg"?"Изтриване на акаунт":"Account deletion"}</a><a href="/legal/acceptable-use">{language==="bg"?"Безопасност":"Safety"}</a><button type="button" onClick={()=>window.dispatchEvent(new Event("inkroute:open-cookie-settings"))}>{language==="bg"?"Настройки за бисквитки":"Cookie preferences"}</button></nav><small>© {new Date().getFullYear()} InkRoute</small></footer>
  </div>;
}
