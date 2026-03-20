import type { ToDo } from '@/types/todo';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/todo';

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

  const fallback = response.status >= 500
    ? `Server error (HTTP ${response.status}) — please try again later.`
    : `Request error (HTTP ${response.status}) — please check your request and try again.`;
  return new Error(fallback);
}

async function safeFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  try {
    return await fetch(input, init);
  } catch {
    throw new Error('Cannot reach the server — check your connection and try again.');
  }
}

export const todoService = {
  async getAll(): Promise<ToDo[]> {
    const response = await safeFetch(API_BASE_URL);
    if (!response.ok) throw await httpError(response, {
      500: 'Server error while loading tasks — try refreshing the page.',
    });
    return response.json();
  },

  async getById(id: string): Promise<ToDo> {
    const response = await safeFetch(`${API_BASE_URL}/${id}`);
    if (!response.ok) throw await httpError(response);
    return response.json();
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
    return response.json();
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
