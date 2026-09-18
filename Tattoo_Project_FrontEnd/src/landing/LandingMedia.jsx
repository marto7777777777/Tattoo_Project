import { useState } from "react";
import { trackLanding } from "./landingTracking";

const getYouTubeId = (src = "") => src.match(/(?:youtu\.be\/|youtube\.com\/(?:watch\?v=|embed\/))([\w-]{11})/)?.[1] || "";

export default function LandingMedia({ video, title, description, priority = false, chapters = [], showPlaceholder = false }) {
  const hasVideo = Boolean(video?.src);
  const youtubeId = getYouTubeId(video?.src);
  const [youtubeStarted, setYouTubeStarted] = useState(false);
  if (!hasVideo && !showPlaceholder) return null;
  return (
    <div className="landing-media-shell">
      {youtubeId ? (youtubeStarted ? <iframe className="landing-video landing-youtube-frame" src={`https://www.youtube-nocookie.com/embed/${youtubeId}?autoplay=1&rel=0`} title={title} allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowFullScreen /> : <button className="landing-youtube-poster" type="button" aria-label={title} onClick={() => { trackLanding("video_play", { video: video.id }); setYouTubeStarted(true); }}><img src="/inkroute-app-icon.png" alt="" width="144" height="144" loading={priority ? "eager" : "lazy"} /><span aria-hidden="true">▶</span></button>) : hasVideo ? <video className="landing-video" controls playsInline preload={priority ? "metadata" : "none"} poster={video.poster || undefined} aria-label={title} onPlay={() => trackLanding("video_play", { video: video.id })}>
          <source src={video.src} />
          {(video.tracks || []).map((track) => <track key={track.src} {...track} />)}
        </video> : <div className="landing-media-placeholder" role="img" aria-label={title}><span className="landing-media-glow" /><img src="/inkroute-app-icon.png" alt="" width="72" height="72" /><strong>{title}</strong><span>{description}</span></div>}
      {chapters.length > 0 && (
        <div className="landing-chapters" aria-label="Video chapters">
          {chapters.map((chapter) => <button type="button" key={chapter} onClick={() => trackLanding("chapter_click", { chapter })}>{chapter}</button>)}
        </div>
      )}
    </div>
  );
}
