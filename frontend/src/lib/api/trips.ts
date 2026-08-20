import type { CreateInviteRequest, CreateTripRequest, InviteResponse, TripResponse, UpdateTripRequest } from "../../types/api";
import { apiRequest } from "./client";

export function listTrips() {
  return apiRequest<TripResponse[]>("/trips");
}

export function getTrip(id: string) {
  return apiRequest<TripResponse>(`/trips/${id}`);
}

export function createTrip(request: CreateTripRequest) {
  return apiRequest<TripResponse>("/trips", {
    method: "POST",
    body: request,
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
}

export function updateTrip(id: string, request: UpdateTripRequest) {
  return apiRequest<TripResponse>(`/trips/${id}`, { method: "PATCH", body: request });
}

export function createInvite(tripId: string, request: CreateInviteRequest) {
  return apiRequest<InviteResponse>(`/trips/${tripId}/invites`, {
    method: "POST",
    body: request,
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
}
