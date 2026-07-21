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
    headers: {},
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function createPaidAiDraft(values, file) {
  const response = await apiRequest("/api/ai-tattoos/paid-draft", {
    method: "POST",
    body: projectForm(values, file),
    headers: {},
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export function generateAiProject(id) {
  return requestJson(`/api/ai-tattoos/${id}/generate`, {
    method: "POST",
  });
}

export function editAiProject(id, instruction, baseVersionId) {
  return requestJson(`/api/ai-tattoos/${id}/edit`, {
    method: "POST",
    body: JSON.stringify({
      instruction,
      baseVersionId,
    }),
  });
}

// Stripe has been removed.

export async function downloadAiVersion(versionId) {
  const response = await apiRequest(
    `/api/ai-tattoos/versions/${versionId}/download`
  );

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || "The image could not be downloaded.");
  }

  return response.blob();
}