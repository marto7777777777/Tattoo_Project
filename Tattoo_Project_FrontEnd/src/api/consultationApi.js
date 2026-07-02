import { apiRequest } from "./http";

export function createConsultation(consultationData) {
  return apiRequest("/api/Consultation", {
    method: "POST",
    body: JSON.stringify(consultationData),
  });
}

export function updateConsultation(id, consultationData) {
  return apiRequest(`/api/Consultation/${id}`, {
    method: "PUT",
    body: JSON.stringify(consultationData),
  });
}

export function completeConsultation(tattooRequestId, data) {
  return apiRequest(`/api/Consultation/complete-consultation/${tattooRequestId}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function rejectConsultation(tattooRequestId) {
  return apiRequest(`/api/Consultation/reject-consultation/${tattooRequestId}`, {
    method: "PUT",
  });
}
