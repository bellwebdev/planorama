import type { ItineraryItemResponse, UpdateItineraryItemRequest } from "../../types/api";
import { apiRequest } from "./client";

export function listItinerary(tripId: string) {
  return apiRequest<ItineraryItemResponse[]>(`/trips/${tripId}/itinerary`);
}

export function updateItineraryItem(id: string, request: UpdateItineraryItemRequest) {
  return apiRequest<ItineraryItemResponse>(`/itinerary-items/${id}`, { method: "PATCH", body: request });
}
