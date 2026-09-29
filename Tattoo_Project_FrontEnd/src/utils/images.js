import { API_BASE_URL } from "../api/apiConfig";

const DEFAULT_IMAGE_OPTIONS = Object.freeze({
  maxWidth: 2048,
  maxHeight: 2048,
  quality: 0.86,
  maxBytes: 4.5 * 1024 * 1024,
});

const SUPPORTED_IMAGE_TYPES = new Set(["image/jpeg", "image/png", "image/webp"]);

export function getImageUrl(imageUrl) {
  if (!imageUrl) return "";
  if (imageUrl.startsWith("http://") || imageUrl.startsWith("https://") || imageUrl.startsWith("data:")) {
    return imageUrl;
  }
  return `${API_BASE_URL}${imageUrl}`;
}

function replaceExtension(fileName, extension) {
  const baseName = (fileName || "inkroute-image").replace(/\.[^/.]+$/, "");
  return `${baseName}${extension}`;
}

function canvasToBlob(canvas, type, quality) {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (blob) => blob ? resolve(blob) : reject(new Error("The browser could not prepare this image.")),
      type,
      quality,
    );
  });
}

async function decodeImage(file) {
  if (typeof createImageBitmap === "function") {
    try {
      const bitmap = await createImageBitmap(file, { imageOrientation: "from-image" });
      return {
        width: bitmap.width,
        height: bitmap.height,
        draw: (context, width, height) => context.drawImage(bitmap, 0, 0, width, height),
        close: () => bitmap.close(),
      };
    } catch {
      // Safari/WebView versions differ in createImageBitmap support. The HTML
      // image fallback below keeps uploads working on those devices.
    }
  }

  const objectUrl = URL.createObjectURL(file);
  try {
    const image = await new Promise((resolve, reject) => {
      const element = new Image();
      element.onload = () => resolve(element);
      element.onerror = () => reject(new Error("The selected file could not be decoded as an image."));
      element.src = objectUrl;
    });
    return {
      width: image.naturalWidth,
      height: image.naturalHeight,
      draw: (context, width, height) => context.drawImage(image, 0, 0, width, height),
      close: () => {},
    };
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

/**
 * Reduces upload CPU time and bandwidth while preserving enough detail for
 * tattoo references and portfolios. This is a performance layer only: the
 * backend must still validate, strip metadata and re-encode every upload.
 */
export async function optimizeImageForUpload(file, options = {}) {
  if (!(file instanceof Blob) || file.size <= 0) {
    throw new Error("Please select a valid image.");
  }

  if (file.type && !SUPPORTED_IMAGE_TYPES.has(file.type.toLowerCase())) {
    throw new Error("Only JPG, PNG and WEBP images are supported.");
  }

  const settings = { ...DEFAULT_IMAGE_OPTIONS, ...options };
  let decoded;
  try {
    decoded = await decodeImage(file);
    if (!decoded.width || !decoded.height) throw new Error("The image has invalid dimensions.");

    const scale = Math.min(
      1,
      settings.maxWidth / decoded.width,
      settings.maxHeight / decoded.height,
    );
    const width = Math.max(1, Math.round(decoded.width * scale));
    const height = Math.max(1, Math.round(decoded.height * scale));

    // Already-small WebP files do not benefit from an extra client-side pass.
    // Skipping it avoids generational quality loss while still bounding the
    // expensive backend decode by pixel dimensions.
    if (
      scale === 1 &&
      file.type?.toLowerCase() === "image/webp" &&
      file.size <= settings.maxBytes
    ) {
      return file;
    }

    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext("2d", { alpha: true });
    if (!context) throw new Error("Image processing is not available in this browser.");
    context.imageSmoothingEnabled = true;
    context.imageSmoothingQuality = "high";
    decoded.draw(context, width, height);

    let quality = settings.quality;
    let blob = await canvasToBlob(canvas, "image/webp", quality);
    while (blob.size > settings.maxBytes && quality > 0.66) {
      quality -= 0.08;
      blob = await canvasToBlob(canvas, "image/webp", quality);
    }

    if (blob.size > settings.maxBytes) {
      throw new Error("The optimized image is still too large. Please choose a smaller image.");
    }

    return new File([blob], replaceExtension(file.name, ".webp"), {
      type: "image/webp",
      lastModified: file.lastModified || Date.now(),
    });
  } catch (error) {
    // Keep older Safari/Android WebViews usable. The backend remains the
    // authoritative decoder and validator, so falling back does not weaken
    // security. Oversized originals are never allowed through this fallback.
    if (file.size <= settings.maxBytes) return file;
    throw error;
  } finally {
    decoded?.close?.();
  }
}

export async function optimizeImagesForUpload(files, options = {}) {
  // Process sequentially. Several large phone photos decoded in parallel can
  // exceed the memory limit of iOS Safari and Android WebView.
  const optimized = [];
  for (const file of files || []) {
    optimized.push(await optimizeImageForUpload(file, options));
  }
  const maxTotalBytes = options.maxTotalBytes ?? 14 * 1024 * 1024;
  if (optimized.reduce((total, file) => total + file.size, 0) > maxTotalBytes) {
    throw new Error("The selected images are too large together. Remove an image or choose smaller files.");
  }
  return optimized;
}
