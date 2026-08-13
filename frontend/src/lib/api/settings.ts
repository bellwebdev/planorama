import type { ReminderOffset, SettingsResponse } from "../../types/api";
import { apiRequest } from "./client";

export function getSettings() {
  return apiRequest<SettingsResponse>("/me/settings");
}

export function updateSettings(reminderOffset: ReminderOffset, notifyEmail: boolean, notifyPush: boolean) {
  return apiRequest<SettingsResponse>("/me/settings", {
    method: "PATCH",
    body: { reminderOffset, notifyEmail, notifyPush },
  });
}
