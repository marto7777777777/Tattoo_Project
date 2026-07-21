import { apiRequest, requestJson } from "./http";

function projectForm(values, file) {
  const form = new FormData();
  form.append("title", values.title);
  form.append("tattooStyle", values.tattooStyle);
  form.append("placement", values.placement);
  form.append("description", values.description);

  if (file) {
    form.append("referenceImage", file);
  }

  return form;
}

export function getAiProjects() {
  return requestJson("/api/ai-tattoos");
}

export function getAiProject(id) {
  return requestJson(`/api/ai-tattoos/${id}`);
}

export async function createFreeAiProject(values, file) {
  const response = await apiRequest("/api/ai-tattoos", {
    method: "POST",
    body: projectForm(values, file),
  });

  const data = await response.json().catch(async () => await response.text());

  if (!response.ok) {
    const message = typeof data === "string"
      ? data
      : data?.message || data?.title || data?.error || "The project could not be created.";
    throw new Error(message);
  }

  return data;
}

export function generateAiProject(id) {
  return requestJson(`/api/ai-tattoos/${id}/generate`, { method: "POST" });
}

export function editAiProject(id, instruction, baseVersionId) {
  return requestJson(`/api/ai-tattoos/${id}/edit`, {
    method: "POST",
    body: JSON.stringify({ instruction, baseVersionId }),
  });
}

export async function downloadAiVersion(versionId) {
  const response = await apiRequest(`/api/ai-tattoos/versions/${versionId}/download`);

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "The image could not be downloaded.");
  }

  return response.blob();
}
