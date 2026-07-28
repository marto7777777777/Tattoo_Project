import { useEffect, useMemo, useState } from "react";
import ImageCropModal from "./ImageCropModal";

function useObjectUrl(file) {
  const url = useMemo(() => (file ? URL.createObjectURL(file) : ""), [file]);
  useEffect(() => () => {
    if (url) URL.revokeObjectURL(url);
  }, [url]);
  return url;
}

function StudioImageSetupFields({
  coverFile,
  logoFile,
  onCoverChange,
  onLogoChange,
}) {
  const [crop, setCrop] = useState(null);
  const coverPreview = useObjectUrl(coverFile);
  const logoPreview = useObjectUrl(logoFile);

  function choose(kind, event) {
    const file = event.target.files?.[0] || null;
    event.target.value = "";
    if (file) setCrop({ kind, file });
  }

  return (
    <>
      <div className="studio-create-media">
        <div className="studio-create-media-heading">
          <h3>Studio images</h3>
          <p className="muted">Add a banner and profile image now. You can change them later.</p>
        </div>

        <div className="studio-create-media-grid">
          <label className="studio-create-media-card studio-create-cover">
            <input type="file" accept="image/*" hidden onChange={(event) => choose("cover", event)} />
            {coverPreview ? <img src={coverPreview} alt="Studio banner preview" /> : <span className="studio-create-media-placeholder">16:9</span>}
            <span className="studio-create-media-action">
              <strong>{coverFile ? "Change banner" : "Choose banner"}</strong>
              <small>Studio banner</small>
            </span>
          </label>

          <label className="studio-create-media-card studio-create-logo">
            <input type="file" accept="image/*" hidden onChange={(event) => choose("logo", event)} />
            {logoPreview ? <img src={logoPreview} alt="Studio profile image preview" /> : <span className="studio-create-media-placeholder">＋</span>}
            <span className="studio-create-media-action">
              <strong>{logoFile ? "Change image" : "Choose image"}</strong>
              <small>Studio profile image</small>
            </span>
          </label>
        </div>
      </div>

      {crop && (
        <ImageCropModal
          file={crop.file}
          title={crop.kind === "cover" ? "Adjust studio banner" : "Adjust studio profile image"}
          shape={crop.kind === "cover" ? "rectangle" : "rounded"}
          aspect={crop.kind === "cover" ? 16 / 9 : 1}
          outputWidth={crop.kind === "cover" ? 1600 : 900}
          onCancel={() => setCrop(null)}
          onConfirm={(file) => {
            if (crop.kind === "cover") onCoverChange?.(file);
            else onLogoChange?.(file);
            setCrop(null);
          }}
        />
      )}
    </>
  );
}

export default StudioImageSetupFields;
