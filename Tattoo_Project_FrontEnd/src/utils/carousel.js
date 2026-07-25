export function moveCarouselPage(track, direction) {
  if (!track || (direction !== -1 && direction !== 1)) return;

  const firstCard = track.querySelector(".visual-carousel-card");
  if (!firstCard) return;

  const styles = window.getComputedStyle(track);
  const gap = Number.parseFloat(styles.columnGap || styles.gap || "0") || 0;
  const cardsPerPage = window.matchMedia("(max-width: 700px)").matches ? 2 : 3;
  const pageWidth = (firstCard.getBoundingClientRect().width + gap) * cardsPerPage;
  const maxScrollLeft = Math.max(0, track.scrollWidth - track.clientWidth);
  const currentScrollLeft = Math.min(maxScrollLeft, Math.max(0, track.scrollLeft));
  const remainingDistance = maxScrollLeft - currentScrollLeft;

  let target;

  if (direction > 0) {
    target =
      remainingDistance <= pageWidth + 2
        ? maxScrollLeft
        : Math.min(maxScrollLeft, currentScrollLeft + pageWidth);
  } else {
    target =
      currentScrollLeft <= pageWidth + 2
        ? 0
        : Math.max(0, currentScrollLeft - pageWidth);
  }

  track.scrollTo({
    left: target,
    behavior: "smooth",
  });
}
