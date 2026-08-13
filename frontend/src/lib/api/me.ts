import type { MeResponse } from "../../types/api";
import { apiRequest } from "./client";

export function getProfile() {
  return apiRequest<MeResponse>("/me");
}

export function updateProfile(displayName: string) {
  return apiRequest<MeResponse>("/me", { method: "PATCH", body: { displayName } });
}

export function uploadAvatar(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  return apiRequest<MeResponse>("/me/avatar", { method: "POST", body: formData });
}
