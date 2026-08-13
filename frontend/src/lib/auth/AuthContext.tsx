import { createContext, useCallback, useContext, useSyncExternalStore, type ReactNode } from "react";
import type { UserSummary } from "../../types/api";
import * as authApi from "../api/auth";
import { clearTokenState, getTokenState, setTokenState, subscribeToTokenState } from "./tokenStore";

interface AuthContextValue {
  user: UserSummary | null;
  isAuthenticated: boolean;
  register: (email: string, password: string, displayName: string) => Promise<void>;
  confirmEmail: (userId: string, token: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  loginWithGoogle: (idToken: string) => Promise<void>;
  resendConfirmation: (email: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { user } = useSyncExternalStore(subscribeToTokenState, getTokenState);

  const login = useCallback(async (email: string, password: string) => {
    const result = await authApi.login(email, password);
    setTokenState({ accessToken: result.tokens.accessToken, refreshToken: result.tokens.refreshToken, user: result.user });
  }, []);

  const loginWithGoogle = useCallback(async (idToken: string) => {
    const result = await authApi.loginWithGoogle(idToken);
    setTokenState({ accessToken: result.tokens.accessToken, refreshToken: result.tokens.refreshToken, user: result.user });
  }, []);

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    await authApi.register(email, password, displayName);
  }, []);

  const confirmEmail = useCallback(async (userId: string, token: string) => {
    await authApi.confirmEmail(userId, token);
  }, []);

  const resendConfirmation = useCallback(async (email: string) => {
    await authApi.resendConfirmation(email);
  }, []);

  const logout = useCallback(async () => {
    const { refreshToken } = getTokenState();
    clearTokenState();
    if (refreshToken) {
      await authApi.logout(refreshToken).catch(() => {});
    }
  }, []);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    register,
    confirmEmail,
    login,
    loginWithGoogle,
    resendConfirmation,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
