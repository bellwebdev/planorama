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
  stayAddress: string;
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
