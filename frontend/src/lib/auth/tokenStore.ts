import type { UserSummary } from "../../types/api";

// React-free session store — the single source of truth for tokens/user, backed by
// localStorage (the project's Bearer-only, no-cookies auth model leaves no other place
// for a JWT to survive a reload). lib/api/client.ts reads/writes this directly so it can
// refresh tokens without importing React state; AuthContext subscribes to expose it reactively.

export interface TokenState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserSummary | null;
}

const STORAGE_KEY = "planorama.auth";
const EMPTY_STATE: TokenState = { accessToken: null, refreshToken: null, user: null };

function loadInitialState(): TokenState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return EMPTY_STATE;
    return JSON.parse(raw) as TokenState;
  } catch {
    return EMPTY_STATE;
  }
}

let state = loadInitialState();
const listeners = new Set<(state: TokenState) => void>();

export function getTokenState(): TokenState {
  return state;
}

export function setTokenState(next: TokenState): void {
  state = next;
  if (next.accessToken && next.refreshToken && next.user) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  } else {
    localStorage.removeItem(STORAGE_KEY);
  }
  listeners.forEach((listener) => listener(state));
}

export function clearTokenState(): void {
  setTokenState(EMPTY_STATE);
}

/** Patches the cached user summary (e.g. after a profile edit) without touching tokens. */
export function updateUser(patch: Partial<UserSummary>): void {
  if (!state.user) return;
  setTokenState({ ...state, user: { ...state.user, ...patch } });
}

export function subscribeToTokenState(listener: (state: TokenState) => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}
