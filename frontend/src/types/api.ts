// Mirrors backend/src/Planorama.Api/Contracts/{Auth,Me}/*.cs 1:1.

export type ReminderOffset = "OneHour" | "TwelveHours" | "TwentyFourHours";

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
}

export interface TokenPairResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  tokenType: string;
}

export interface LoginResponse {
  user: UserSummary;
  tokens: TokenPairResponse;
}

export interface RegisterResponse {
  userId: string;
  email: string;
}

export interface MeResponse {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  createdAt: string;
}

export interface SettingsResponse {
  reminderOffset: ReminderOffset;
  notifyEmail: boolean;
  notifyPush: boolean;
}

// Mirrors backend/src/Planorama.Api/Contracts/Trips/*.cs 1:1.

export type TripStatus = "Draft" | "Planning" | "Active" | "Completed";
export type InvitedVia = "Link" | "Email";

export interface TripResponse {
  id: string;
  creatorId: string;
  name: string;
  description: string | null;
  locationName: string;
  locationLat: number | null;
  locationLng: number | null;
  stayAddress: string;
  stayLat: number | null;
  stayLng: number | null;
  startDate: string;
  endDate: string;
  timezone: string;
  status: TripStatus;
  defaultVotingWindowHours: number;
  createdAt: string;
}

export interface CreateTripRequest {
  name: string;
  description?: string | null;
  locationName: string;
  stayAddress: string;
  startDate: string;
  endDate: string;
  timezone: string;
  defaultVotingWindowHours?: number | null;
}

export interface UpdateTripRequest {
  name: string;
  description?: string | null;
  locationName: string;
  stayAddress: string;
  startDate: string;
  endDate: string;
  timezone: string;
  defaultVotingWindowHours: number;
  status: TripStatus;
}

export interface InviteResponse {
  token: string;
  invitedVia: InvitedVia;
  contact: string | null;
  expiresAt: string;
}

export interface CreateInviteRequest {
  via: InvitedVia;
  contact?: string | null;
}

// Mirrors backend/src/Planorama.Api/Contracts/Places/*.cs 1:1.

export type PlaceCategory =
  | "Restaurant"
  | "Cafe"
  | "Museum"
  | "Attraction"
  | "Sights"
  | "Park"
  | "Playground"
  | "Beach"
  | "Zoo"
  | "ThemePark"
  | "Shopping"
  | "Nature";

export type TravelMode = "Drive" | "Walk" | "Bicycle" | "Transit";

export interface PlaceCategoryResponse {
  value: PlaceCategory;
  label: string;
}

export interface PlaceResponse {
  providerPlaceId: string;
  name: string;
  lat: number;
  lng: number;
  category: PlaceCategory;
  address: string | null;
  distanceMeters: number | null;
  /** Always null — the current provider (Geoapify) carries no rating score. */
  rating: number | null;
}

export interface PlaceDetailResponse {
  providerPlaceId: string;
  name: string;
  lat: number;
  lng: number;
  category: PlaceCategory | null;
  address: string | null;
  description: string | null;
  website: string | null;
  rating: number | null;
}

export interface RouteResponse {
  distanceMeters: number;
  durationSeconds: number;
  mode: TravelMode;
  /** Raw GeoJSON geometry (typically a LineString), ready to hand to a map layer. */
  geometry: string | null;
}

// Mirrors backend/src/Planorama.Api/Contracts/Suggestions/*.cs 1:1.

export type SuggestionSource = "Custom" | "Geoapify";
export type SuggestionStatus = "Voting" | "Approved" | "Discarded" | "Expired";
export type SuggestionResolution = "Majority" | "CoinFlip" | "NoQuorum" | "Manual";
export type VoteValue = "Yes" | "No";

export interface VoteResponse {
  userId: string;
  displayName: string;
  value: VoteValue;
  castAt: string;
}

/** yesCount/noCount/votes are null until the requesting member has cast their own vote — the API
 * withholds them server-side (spec §6.3), not the UI. */
export interface SuggestionResponse {
  id: string;
  tripId: string;
  suggestedById: string;
  suggestedByName: string;
  source: SuggestionSource;
  providerPlaceId: string | null;
  title: string;
  description: string | null;
  lat: number | null;
  lng: number | null;
  address: string | null;
  externalRating: number | null;
  proposedDate: string | null;
  proposedStartTime: string | null;
  durationMinutes: number | null;
  votingClosesAt: string;
  status: SuggestionStatus;
  resolution: SuggestionResolution | null;
  resolvedAt: string | null;
  createdAt: string;
  hasVoted: boolean;
  yourVote: VoteValue | null;
  yesCount: number | null;
  noCount: number | null;
  votes: VoteResponse[] | null;
}

export interface CreateSuggestionRequest {
  providerPlaceId?: string | null;
  title?: string | null;
  description?: string | null;
  address?: string | null;
  proposedDate?: string | null;
  proposedStartTime?: string | null;
  durationMinutes?: number | null;
  votingClosesAt?: string | null;
}

export interface CastVoteRequest {
  value: VoteValue;
}
