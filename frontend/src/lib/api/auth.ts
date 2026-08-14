import type { LoginResponse, RegisterResponse, TokenPairResponse } from "../../types/api";
import { apiRequest } from "./client";

export function register(email: string, password: string, displayName: string, turnstileToken: string) {
  return apiRequest<RegisterResponse>("/auth/register", {
    method: "POST",
    auth: false,
    body: { email, password, displayName, turnstileToken },
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
}

export function confirmEmail(userId: string, token: string) {
  return apiRequest<void>("/auth/confirm-email", { method: "POST", auth: false, body: { userId, token } });
}

export function login(email: string, password: string) {
  return apiRequest<LoginResponse>("/auth/login", { method: "POST", auth: false, body: { email, password } });
}

export function loginWithGoogle(idToken: string) {
  return apiRequest<LoginResponse>("/auth/external/google", { method: "POST", auth: false, body: { idToken } });
}

export function refresh(refreshToken: string) {
  return apiRequest<TokenPairResponse>("/auth/refresh", { method: "POST", auth: false, body: { refreshToken } });
}

export function logout(refreshToken: string) {
  return apiRequest<void>("/auth/logout", { method: "POST", auth: false, body: { refreshToken } });
}

export function resendConfirmation(email: string, turnstileToken: string) {
  return apiRequest<void>("/auth/resend-confirmation", {
    method: "POST",
    auth: false,
    body: { email, turnstileToken },
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });
}
