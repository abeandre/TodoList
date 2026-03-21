import type { UserResponse, CreateUserRequest, LoginRequest } from '@/types/user';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const REQUEST_TIMEOUT_MS = 10_000;

async function httpError(response: Response, overrides: Partial<Record<number, string>> = {}): Promise<Error> {
  if (overrides[response.status]) return new Error(overrides[response.status]);

  try {
    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
      const body = await response.json() as { title?: string; detail?: string };
      const detail = body?.detail ?? body?.title;
      if (detail) return new Error(detail);
    }
  } catch {
    // ignore parse errors
  }

  const fallback = response.status >= 500
    ? 'A server error occurred — please try again later.'
    : 'The request could not be completed — please check your credentials and try again.';
  return new Error(fallback);
}

async function safeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    return await fetch(input, { ...init, signal: controller.signal });
  } finally {
    clearTimeout(timeoutId);
  }
}

async function parseJson<T>(response: Response): Promise<T> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) {
    throw new Error('Unexpected response format from server.');
  }
  return await response.json() as T;
}

export const authService = {
  getToken(): string | null {
    return localStorage.getItem('jwt');
  },

  setToken(token: string): void {
    localStorage.setItem('jwt', token);
  },

  clearToken(): void {
    localStorage.removeItem('jwt');
  },

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  },

  async login(request: LoginRequest): Promise<UserResponse> {
    const response = await safeFetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw await httpError(response, {
        400: 'Invalid login request.',
        401: 'Invalid username or password.',
      });
    }

    const data = await parseJson<UserResponse>(response);
    if (data.token) {
      this.setToken(data.token);
    }
    return data;
  },

  async register(request: CreateUserRequest): Promise<UserResponse> {
    const response = await safeFetch(`${API_BASE_URL}/user`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw await httpError(response, {
        400: 'Could not register user — perhaps the username or email is already taken.',
        409: 'Username or email already exists.',
      });
    }

    const data = await parseJson<UserResponse>(response);
    if (data.token) {
      this.setToken(data.token);
    }
    return data;
  },

  logout(): void {
    this.clearToken();
    // Redirect logic will be handled by the Vue app/router
  }
};
