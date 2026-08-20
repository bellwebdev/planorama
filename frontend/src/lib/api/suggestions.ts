import type { CastVoteRequest, CreateSuggestionRequest, SuggestionResponse } from "../../types/api";
import { apiRequest } from "./client";

export function listSuggestions(tripId: string) {
  return apiRequest<SuggestionResponse[]>(`/trips/${tripId}/suggestions`);
}

export function getSuggestion(id: string) {
  return apiRequest<SuggestionResponse>(`/suggestions/${id}`);
}

export function createSuggestion(tripId: string, request: CreateSuggestionRequest) {
  return apiRequest<SuggestionResponse>(`/trips/${tripId}/suggestions`, {
    method: "POST",
    body: request,
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
}

export function castVote(suggestionId: string, request: CastVoteRequest) {
  return apiRequest<SuggestionResponse>(`/suggestions/${suggestionId}/vote`, { method: "PUT", body: request });
}
