import { useEffect, useMemo, useRef, useState } from "react";

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function ImageCropModal({
  file,
  aspect = 1,
  shape = "circle",
  title = "Adjust image",
  outputWidth = 900,
  onCancel,
  onConfirm,
}) {
  const viewportRef = useRef(null);
  const dragRef = useRef(null);
  const [sourceUrl, setSourceUrl] = useState("");
  const [naturalSize, setNaturalSize] = useState({ width: 0, height: 0 });
  const [viewportSize, setViewportSize] = useState({ width: 0, height: 0 });
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [saving, setSaving] = useState(false);
  const [cropError, setCropError] = useState("");

  useEffect(() => {
    if (!file) return undefined;
    const url = URL.createObjectURL(file);
    setSourceUrl(url);
    setZoom(1);
    setOffset({ x: 0, y: 0 });
    setCropError("");
    return () => URL.revokeObjectURL(url);
  }, [file]);

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return undefined;
    const updateSize = () => {
      const rect = viewport.getBoundingClientRect();
      setViewportSize({ width: rect.width, height: rect.height });
    };
    updateSize();
    const observer = new ResizeObserver(updateSize);
    observer.observe(viewport);
    return () => observer.disconnect();
  }, [aspect]);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    const closeOnEscape = (event) => {
      if (event.key === "Escape" && !saving) onCancel?.();
    };
    window.addEventListener("keydown", closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", closeOnEscape);
    };
  }, [onCancel, saving]);

  const baseScale = useMemo(() => {
    if (!naturalSize.width || !viewportSize.width) return 1;
    return Math.max(
      viewportSize.width / naturalSize.width,
      viewportSize.height / naturalSize.height,
    );
  }, [naturalSize, viewportSize]);

  const displayedSize = useMemo(() => ({
    width: naturalSize.width * baseScale * zoom,
    height: naturalSize.height * baseScale * zoom,
  }), [naturalSize, baseScale, zoom]);

  function clampOffset(nextOffset, nextDisplayedSize = displayedSize) {
    const maxX = Math.max(0, (nextDisplayedSize.width - viewportSize.width) / 2);
    const maxY = Math.max(0, (nextDisplayedSize.height - viewportSize.height) / 2);
    return {
      x: clamp(nextOffset.x, -maxX, maxX),
      y: clamp(nextOffset.y, -maxY, maxY),
    };
  }

  function handleZoom(nextZoom) {
    const safeZoom = clamp(Number(nextZoom), 1, 3);
    const nextSize = {
      width: naturalSize.width * baseScale * safeZoom,
      height: naturalSize.height * baseScale * safeZoom,
    };
    setZoom(safeZoom);
    setOffset((current) => clampOffset(current, nextSize));
  }

  function handlePointerDown(event) {
    if (!naturalSize.width) return;
    event.currentTarget.setPointerCapture?.(event.pointerId);
    dragRef.current = {
      pointerId: event.pointerId,
      x: event.clientX,
      y: event.clientY,
      offset,
    };
  }

  function handlePointerMove(event) {
    const drag = dragRef.current;
    if (!drag || drag.pointerId !== event.pointerId) return;
    setOffset(clampOffset({
      x: drag.offset.x + event.clientX - drag.x,
      y: drag.offset.y + event.clientY - drag.y,
    }));
  }

  function handlePointerUp(event) {
    if (dragRef.current?.pointerId === event.pointerId) dragRef.current = null;
  }

  async function confirmCrop() {
    if (!naturalSize.width || !viewportSize.width || saving) return;
    setSaving(true);
    setCropError("");
    try {
      const displayScale = baseScale * zoom;
      const sourceWidth = viewportSize.width / displayScale;
      const sourceHeight = viewportSize.height / displayScale;
      const sourceX = naturalSize.width / 2 - offset.x / displayScale - sourceWidth / 2;
      const sourceY = naturalSize.height / 2 - offset.y / displayScale - sourceHeight / 2;
      const outputHeight = Math.round(outputWidth / aspect);
      const canvas = document.createElement("canvas");
      canvas.width = outputWidth;
      canvas.height = outputHeight;
      const context = canvas.getContext("2d", { alpha: false });
      const image = new Image();
      image.src = sourceUrl;
      await image.decode();
      context.imageSmoothingEnabled = true;
      context.imageSmoothingQuality = "high";
      context.drawImage(
        image,
        sourceX,
        sourceY,
        sourceWidth,
        sourceHeight,
        0,
        0,
        outputWidth,
        outputHeight,
      );
      const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.92));
      if (!blob) throw new Error("Could not prepare the cropped image.");
      const baseName = file.name.replace(/\.[^.]+$/, "") || "image";
      await onConfirm?.(new File([blob], `${baseName}-cropped.jpg`, {
        type: "image/jpeg",
        lastModified: Date.now(),
      }));
    } catch (error) {
      setCropError(error?.message || "Could not prepare the cropped image.");
    } finally {
      setSaving(false);
    }
  }

  if (!file) return null;

  return (
    <div className="modal-backdrop image-crop-backdrop" role="presentation" onMouseDown={(event) => {
      if (event.target === event.currentTarget && !saving) onCancel?.();
    }}>
      <section className="modal-card image-crop-modal" role="dialog" aria-modal="true" aria-labelledby="image-crop-title">
        <div className="image-crop-heading">
          <div>
            <p className="subtitle">Image position</p>
            <h2 id="image-crop-title">{title}</h2>
            <p>Move the image inside the template and zoom until the important part fits.</p>
          </div>
          <button className="image-crop-close" type="button" onClick={onCancel} disabled={saving} aria-label="Close">×</button>
        </div>

        <div className="image-crop-stage">
          <div
            ref={viewportRef}
            className={`image-crop-viewport image-crop-${shape}`}
            style={{ aspectRatio: String(aspect) }}
            onPointerDown={handlePointerDown}
            onPointerMove={handlePointerMove}
            onPointerUp={handlePointerUp}
            onPointerCancel={handlePointerUp}
          >
            {sourceUrl && (
              <img
                src={sourceUrl}
                alt=""
                draggable="false"
                onLoad={(event) => setNaturalSize({
                  width: event.currentTarget.naturalWidth,
                  height: event.currentTarget.naturalHeight,
                })}
                style={{
                  width: displayedSize.width || "auto",
                  height: displayedSize.height || "auto",
                  left: `calc(50% + ${offset.x}px)`,
                  top: `calc(50% + ${offset.y}px)`,
                }}
              />
            )}
            <span className="image-crop-template" aria-hidden="true" />
          </div>
          <small>Drag to reposition</small>
        </div>

        <label className="image-crop-zoom">
          <span>Zoom</span>
          <input
            type="range"
            min="1"
            max="3"
            step="0.01"
            value={zoom}
            onChange={(event) => handleZoom(event.target.value)}
          />
          <strong>{Math.round(zoom * 100)}%</strong>
        </label>

        {cropError && <p className="error image-crop-error">{cropError}</p>}

        <div className="image-crop-actions">
          <button className="secondary-button" type="button" onClick={onCancel} disabled={saving}>Cancel</button>
          <button className="primary-button" type="button" onClick={confirmCrop} disabled={saving || !naturalSize.width}>
            {saving ? "Preparing..." : "Use image"}
          </button>
        </div>
      </section>
    </div>
  );
}

export default ImageCropModal;
