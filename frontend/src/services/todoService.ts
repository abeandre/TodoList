import type { ToDo } from '@/types/todo';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/todo';

// abort requests that stall longer than this
const REQUEST_TIMEOUT_MS = 10_000;

const DEFAULT_STATUS_MESSAGES: Partial<Record<number, string>> = {
  400: 'The request was invalid — title is required and must be under 200 characters, description under 2000.',
  404: 'The task no longer exists — it may have been deleted.',
  500: 'A server error occurred — please try again later.',
};

async function httpError(response: Response, overrides: Partial<Record<number, string>> = {}): Promise<Error> {
  if (overrides[response.status]) return new Error(overrides[response.status]);
  if (DEFAULT_STATUS_MESSAGES[response.status]) return new Error(DEFAULT_STATUS_MESSAGES[response.status]);

  // Try to extract a message from the response body (e.g. ASP.NET ValidationProblemDetails)
  try {
    const contentType = response.headers.get('content-type') ?? '';
    if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
      const body = await response.json() as { title?: string; detail?: string };
      const detail = body?.detail ?? body?.title;
      if (detail) return new Error(detail);
    }
  } catch {
    // ignore parse errors — fall through to generic message
  }

  // generic fallback; do not expose raw HTTP status codes to the user
  const fallback = response.status >= 500
    ? 'A server error occurred — please try again later.'
    : 'The request could not be completed — please check your input and try again.';
  return new Error(fallback);
}

// wraps fetch with a timeout; throws a user-friendly error on stall or network failure
async function safeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    return await fetch(input, { ...init, signal: controller.signal });
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError')
      throw new Error('The request timed out — please try again.');
    throw new Error('Cannot reach the server — check your connection and try again.');
  } finally {
    clearTimeout(timeoutId);
  }
}

// type guard for a single ToDo
function isToDo(val: unknown): val is ToDo {
  if (typeof val !== 'object' || val === null) return false;
  const t = val as Record<string, unknown>;
  return (
    typeof t.id === 'string' &&
    typeof t.title === 'string' &&
    typeof t.description === 'string' &&
    typeof t.createdAt === 'string' &&
    typeof t.updatedAt === 'string' &&
    (t.finishedAt === null || typeof t.finishedAt === 'string')
  );
}

function isToDoArray(val: unknown): val is ToDo[] {
  return Array.isArray(val) && val.every(isToDo);
}

// verify Content-Type before parsing, then validate shape at runtime
async function parseJson<T>(response: Response, guard: (val: unknown) => val is T): Promise<T> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) {
    throw new Error('Unexpected response format from server.');
  }
  const data: unknown = await response.json();
  if (!guard(data)) throw new Error('Unexpected data shape received from server.');
  return data;
}

export const todoService = {
  async getAll(): Promise<ToDo[]> {
    const response = await safeFetch(API_BASE_URL);
    if (!response.ok) throw await httpError(response, {
      500: 'Server error while loading tasks — try refreshing the page.',
    });
    return parseJson(response, isToDoArray);
  },

  async create(todo: Pick<ToDo, 'title' | 'description'>): Promise<ToDo> {
    const response = await safeFetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(todo),
    });
    if (!response.ok) throw await httpError(response, {
      400: 'Could not create the task — title is required and must be under 200 characters, description under 2000.',
      500: 'Server error while creating the task — please try again.',
    });
    return parseJson(response, isToDo);
  },

  async update(id: string, data: Pick<ToDo, 'title' | 'description'>): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!response.ok) throw await httpError(response, {
      400: 'Could not save changes — title is required and must be under 200 characters, description under 2000.',
      404: 'This task no longer exists — it may have been deleted by someone else.',
      500: 'Server error while saving changes — please try again.',
    });
  },

  async changeStatus(id: string, isCompleted: boolean): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isCompleted }),
    });
    if (!response.ok) throw await httpError(response, {
      404: 'This task no longer exists — it may have been deleted.',
      500: 'Server error while updating the task status — please try again.',
    });
  },

  async delete(id: string): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
    if (!response.ok) throw await httpError(response, {
      404: 'This task has already been deleted.',
      500: 'Server error while deleting the task — please try again.',
    });
  },
};
