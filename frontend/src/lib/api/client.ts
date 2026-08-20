import type { TokenPairResponse } from "../../types/api";
import { clearTokenState, getTokenState, setTokenState } from "../auth/tokenStore";

const API_BASE = `${import.meta.env.VITE_API_BASE_URL ?? ""}/api/v1`;

/** RFC 7807 problem+json (AuthProblemException/BadHttpRequestException) plus the
 * FluentValidation `errors` extension (ValidationFilter<T> / Results.ValidationProblem). */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;
  readonly fieldErrors?: Record<string, string[]>;

  constructor(status: number, title: string, detail?: string, fieldErrors?: Record<string, string[]>) {
    super(detail ?? title);
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.fieldErrors = fieldErrors;
  }
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  headers?: Record<string, string>;
  /** Attach the access token and allow refresh-on-401. Default true; auth endpoints pass false. */
  auth?: boolean;
  signal?: AbortSignal;
}

interface ProblemPayload {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function parseErrorResponse(response: Response): Promise<ApiError> {
  let payload: ProblemPayload | null = null;
  try {
    payload = await response.json();
  } catch {
    // No JSON body (e.g. a raw framework 401/404) — fall back to the status text below.
  }
  return new ApiError(
    response.status,
    payload?.title ?? response.statusText ?? "Request failed",
    payload?.detail,
    payload?.errors,
  );
}

function rawRequest(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = { ...options.headers };
  let body: BodyInit | undefined;

  if (options.body instanceof FormData) {
    body = options.body;
  } else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }

  if (options.auth !== false) {
    const { accessToken } = getTokenState();
    if (accessToken) {
      headers["Authorization"] = `Bearer ${accessToken}`;
    }
  }

  return fetch(`${API_BASE}${path}`, { method: options.method ?? "GET", headers, body, signal: options.signal });
}

let refreshPromise: Promise<boolean> | null = null;

function refreshTokens(): Promise<boolean> {
  const { refreshToken, user } = getTokenState();
  if (!refreshToken || !user) return Promise.resolve(false);

  refreshPromise ??= (async () => {
    const response = await rawRequest("/auth/refresh", { method: "POST", auth: false, body: { refreshToken } });
    if (!response.ok) return false;
    const tokens: TokenPairResponse = await response.json();
    setTokenState({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken, user });
    return true;
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await rawRequest(path, options);

  if (response.status === 401 && options.auth !== false) {
    const refreshed = await refreshTokens();
    if (refreshed) {
      response = await rawRequest(path, options);
    } else {
      clearTokenState();
      throw await parseErrorResponse(response);
    }
  }

  if (!response.ok) {
    throw await parseErrorResponse(response);
  }

  if (response.status === 204 || response.status === 202) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
