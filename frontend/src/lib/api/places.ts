import type { PlaceCategory, PlaceCategoryResponse, PlaceDetailResponse, PlaceResponse, RouteResponse, TravelMode } from "../../types/api";
import { apiRequest } from "./client";

export function listPlaceCategories() {
  return apiRequest<PlaceCategoryResponse[]>("/places/categories");
}

export interface SearchPlacesParams {
  category: PlaceCategory;
  radius?: number;
  q?: string;
  limit?: number;
}

export function searchPlacesNearStay(tripId: string, params: SearchPlacesParams, signal?: AbortSignal) {
  const query = new URLSearchParams({ category: params.category });
  if (params.radius !== undefined) query.set("radius", String(params.radius));
  if (params.q) query.set("q", params.q);
  if (params.limit !== undefined) query.set("limit", String(params.limit));

  return apiRequest<PlaceResponse[]>(`/trips/${tripId}/places/search?${query.toString()}`, { signal });
}

export function getPlaceDetail(providerPlaceId: string) {
  return apiRequest<PlaceDetailResponse>(`/places/${encodeURIComponent(providerPlaceId)}`);
}

export function getRouteFromStay(tripId: string, toLat: number, toLng: number, mode: TravelMode) {
  const query = new URLSearchParams({ toLat: String(toLat), toLng: String(toLng), mode });
  return apiRequest<RouteResponse>(`/trips/${tripId}/route?${query.toString()}`);
}
