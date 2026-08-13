// Mirrors backend/src/Planorama.Api/Contracts/{Auth,Me}/*.cs 1:1.

export type ReminderOffset = "OneHour" | "TwelveHours" | "TwentyFourHours";

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
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
