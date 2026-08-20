import type { TripResponse } from "../../types/api";
import { apiRequest } from "./client";

export function acceptInvite(token: string) {
  return apiRequest<TripResponse>(`/invites/${token}/accept`, { method: "POST" });
}
