import { useState } from "react";
import "../styles/helpGuides.css";

const guides = [
  {
    id: "ayKkQlYRhA4",
    title: "Create your InkRoute artist profile",
    description: "Set up your artist profile, studio, portfolio, requirements and weekly schedule.",
  },
  {
    id: "s3p5396zidg",
    title: "Watch the full InkRoute workflow",
    description: "See the complete process from the client's first request to the artist's calendar.",
  },
];

export default function HelpGuidesPage() {
  const [started, setStarted] = useState({});
  return (
    <main className="help-guides-page">
      <header className="help-guides-header">
        <span>Video tutorials</span>
        <h1>Help &amp; guides</h1>
        <p>Learn how to set up your artist profile and manage a complete tattoo project in InkRoute.</p>
      </header>

      <section className="help-guides-grid" aria-label="Video tutorials">
        {guides.map((guide) => (
          <article className="help-guide-card" key={guide.id}>
            <div className="help-guide-video">
              {started[guide.id] ? <iframe
                src={`https://www.youtube-nocookie.com/embed/${guide.id}?rel=0`}
                title={guide.title}
                loading="lazy"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                allowFullScreen
              /> : <button type="button" className="help-guide-placeholder" onClick={() => setStarted((value) => ({ ...value, [guide.id]: true }))} aria-label={`Play ${guide.title}`}><img src="/inkroute-app-icon.png" width="96" height="96" alt="" /><span>▶ Play tutorial</span></button>}
            </div>
            <div className="help-guide-copy">
              <h2>{guide.title}</h2>
              <p>{guide.description}</p>
            </div>
          </article>
        ))}
      </section>

      <p className="help-guides-caption">Open the video menu to choose subtitles in your preferred language.</p>
    </main>
  );
}
