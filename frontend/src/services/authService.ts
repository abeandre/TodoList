import type { UserResponse, CreateUserRequest, LoginRequest } from '@/types/user';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const REQUEST_TIMEOUT_MS = 10_000;

interface StoredUser {
  id: string;
  name: string;
  email: string;
}

function getStoredUser(): StoredUser | null {
  try {
    const raw = localStorage.getItem('user');
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  } catch {
    return null;
  }
}

async function httpError(response: Response, overrides: Partial<Record<number, string>> = {}): Promise<Error> {
  if (overrides[response.status]) return new Error(overrides[response.status]);

  try {
    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
      const body = await response.json();
      if (typeof body === 'string' && body) return new Error(body);
      const detail = body?.detail ?? body?.title;
      if (detail) return new Error(detail);
    }
  } catch {
    // ignore parse errors
  }

  if (response.status === 429) return new Error('Too many requests — please wait a moment before trying again.');
  const fallback = response.status >= 500
    ? 'A server error occurred — please try again later.'
    : 'The request could not be completed — please check your credentials and try again.';
  return new Error(fallback);
}

async function safeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    return await fetch(input, { ...init, credentials: 'include', signal: controller.signal });
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
  isAuthenticated(): boolean {
    return getStoredUser() !== null;
  },

  getUserId(): string | null {
    return getStoredUser()?.id ?? null;
  },

  clearUser(): void {
    localStorage.removeItem('user');
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
        401: 'Invalid email or password.',
        429: 'Too many login attempts — please wait a minute before trying again.',
      });
    }

    const data = await parseJson<UserResponse>(response);
    localStorage.setItem('user', JSON.stringify({ id: data.id, name: data.name, email: data.email }));
    return data;
  },

  async register(request: CreateUserRequest): Promise<UserResponse> {
    const response = await safeFetch(`${API_BASE_URL}/user`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      // No 400 override — let the server message (e.g. "Email is already registered") surface directly.
      throw await httpError(response);
    }

    const data = await parseJson<UserResponse>(response);
    localStorage.setItem('user', JSON.stringify({ id: data.id, name: data.name, email: data.email }));
    return data;
  },

  async logout(): Promise<void> {
    try {
      await safeFetch(`${API_BASE_URL}/auth/logout`, { method: 'POST' });
    } finally {
      this.clearUser();
    }
  },

  async deleteAccount(id: string): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/user/${id}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw await httpError(response, {
        403: 'You can only delete your own account.',
        404: 'Account not found.',
        429: 'Too many requests — please wait a moment before trying again.',
      });
    }

    this.clearUser();
  },
};
