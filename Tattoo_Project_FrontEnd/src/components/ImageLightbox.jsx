import { useEffect } from "react";

function ImageLightbox({ imageUrl, alt = "Portfolio image", onClose }) {
  useEffect(() => {
    if (!imageUrl) return undefined;

    function handleKeyDown(event) {
      if (event.key === "Escape") onClose?.();
    }

    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = "";
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [imageUrl, onClose]);

  if (!imageUrl) return null;

  return (
    <div className="portfolio-lightbox" role="dialog" aria-modal="true" aria-label="Full size portfolio image" onClick={onClose}>
      <button className="portfolio-lightbox-close" type="button" onClick={onClose} aria-label="Close image">×</button>
      <img src={imageUrl} alt={alt} onClick={(event) => event.stopPropagation()} />
    </div>
  );
}

export default ImageLightbox;
