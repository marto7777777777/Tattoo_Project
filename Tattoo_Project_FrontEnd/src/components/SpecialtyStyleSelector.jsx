import { useEffect, useRef, useState } from "react";
import { TATTOO_STYLES } from "../data/tattooOptions";

function SpecialtyStyleSelector({ value = [], onChange }) {
  const trackRef = useRef(null);
  const [canLeft, setCanLeft] = useState(false);
  const [canRight, setCanRight] = useState(true);

  function syncArrows() {
    const track = trackRef.current;
    if (!track) return;
    const max = Math.max(0, track.scrollWidth - track.clientWidth);
    const position = Math.max(0, Math.min(max, track.scrollLeft));
    const edgeTolerance = 3;
    setCanLeft(position > edgeTolerance);
    setCanRight(max > edgeTolerance && position < max - edgeTolerance);
  }

  useEffect(() => {
    syncArrows();
    const track = trackRef.current;
    if (!track) return undefined;
    const observer = new ResizeObserver(syncArrows);
    observer.observe(track);
    Array.from(track.children).forEach((item) => observer.observe(item));
    window.addEventListener("resize", syncArrows);
    const frame = window.requestAnimationFrame(syncArrows);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", syncArrows);
      window.cancelAnimationFrame(frame);
    };
  }, []);

  function move(direction) {
    const track = trackRef.current;
    if (!track) return;
    const card = track.querySelector(".specialty-style-option");
    const step = card ? card.getBoundingClientRect().width + 12 : track.clientWidth * 0.8;
    const max = Math.max(0, track.scrollWidth - track.clientWidth);
    const target = Math.max(0, Math.min(max, track.scrollLeft + direction * step));
    track.scrollTo({ left: target, behavior: "smooth" });
    window.setTimeout(syncArrows, 420);
  }

  function toggle(style) {
    const selected = value.includes(style);
    if (!selected && value.length >= 8) return;
    onChange?.(selected ? value.filter((item) => item !== style) : [...value, style]);
  }

  return (
    <div className="specialty-style-selector">
      {canLeft && <button className="specialty-style-arrow left" type="button" onClick={() => move(-1)} aria-label="Previous styles">‹</button>}
      <div className="specialty-style-track" ref={trackRef} onScroll={syncArrows}>
        {TATTOO_STYLES.map((style) => {
          const selected = value.includes(style.value);
          return (
            <button
              className={`specialty-style-option ${selected ? "selected" : ""}`}
              type="button"
              aria-pressed={selected}
              disabled={!selected && value.length >= 8}
              key={style.value}
              onClick={() => toggle(style.value)}
            >
              <img src={style.image} alt="" />
              <span>{style.value}</span>
              <b>{selected ? "✓" : "+"}</b>
            </button>
          );
        })}
      </div>
      {canRight && <button className="specialty-style-arrow right" type="button" onClick={() => move(1)} aria-label="Next styles">›</button>}
    </div>
  );
}

export default SpecialtyStyleSelector;
