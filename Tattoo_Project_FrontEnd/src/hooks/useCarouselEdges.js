import { useEffect, useState } from "react";

export function useCarouselEdges(trackRef, itemCount) {
  const [edges, setEdges] = useState({
    canScrollLeft: false,
    canScrollRight: false,
  });

  useEffect(() => {
    const track = trackRef.current;
    if (!track) return undefined;

    let frameId = 0;

    const updateEdges = () => {
      cancelAnimationFrame(frameId);
      frameId = requestAnimationFrame(() => {
        const cards = track.querySelectorAll(".visual-carousel-card");
        const firstCard = cards[0];
        const lastCard = cards[cards.length - 1];
        const trackRect = track.getBoundingClientRect();
        const styles = window.getComputedStyle(track);
        const visibleLeft =
          trackRect.left + (Number.parseFloat(styles.paddingLeft) || 0);
        const visibleRight =
          trackRect.right - (Number.parseFloat(styles.paddingRight) || 0);
        const tolerance = 2;

        setEdges({
          canScrollLeft: Boolean(
            firstCard &&
              firstCard.getBoundingClientRect().left < visibleLeft - tolerance
          ),
          canScrollRight: Boolean(
            lastCard &&
              lastCard.getBoundingClientRect().right > visibleRight + tolerance
          ),
        });
      });
    };

    updateEdges();
    track.addEventListener("scroll", updateEdges, { passive: true });
    window.addEventListener("resize", updateEdges);

    const resizeObserver =
      typeof ResizeObserver === "undefined"
        ? null
        : new ResizeObserver(updateEdges);

    resizeObserver?.observe(track);

    return () => {
      cancelAnimationFrame(frameId);
      track.removeEventListener("scroll", updateEdges);
      window.removeEventListener("resize", updateEdges);
      resizeObserver?.disconnect();
    };
  }, [trackRef, itemCount]);

  return edges;
}
