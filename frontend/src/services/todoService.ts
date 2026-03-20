import type { ToDo } from '@/types/todo';

const API_BASE_URL = '/api/todo';

const DEFAULT_STATUS_MESSAGES: Partial<Record<number, string>> = {
  400: 'The request was invalid — check that the title is not empty and under 200 characters.',
  404: 'The task no longer exists — it may have been deleted.',
  500: 'A server error occurred — please try again later.',
};

function httpError(response: Response, overrides: Partial<Record<number, string>> = {}): Error {
  const message =
    overrides[response.status] ??
    DEFAULT_STATUS_MESSAGES[response.status] ??
    `Unexpected error (HTTP ${response.status}) — please try again.`;
  return new Error(message);
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
    if (!response.ok) throw httpError(response, {
      500: 'Server error while loading tasks — try refreshing the page.',
    });
    return response.json();
  },

  async getById(id: string): Promise<ToDo> {
    const response = await safeFetch(`${API_BASE_URL}/${id}`);
    if (!response.ok) throw httpError(response);
    return response.json();
  },

  async create(todo: Pick<ToDo, 'title' | 'description'>): Promise<ToDo> {
    const response = await safeFetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(todo),
    });
    if (!response.ok) throw httpError(response, {
      400: 'Could not create the task — title is required and must be under 200 characters.',
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
    if (!response.ok) throw httpError(response, {
      400: 'Could not save changes — title is required and must be under 200 characters.',
      404: 'This task no longer exists — it may have been deleted by someone else.',
      500: 'Server error while saving changes — please try again.',
    });
  },

  async changeStatus(id: string, isCompleted: boolean): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(isCompleted),
    });
    if (!response.ok) throw httpError(response, {
      404: 'This task no longer exists — it may have been deleted.',
      500: 'Server error while updating the task status — please try again.',
    });
  },

  async delete(id: string): Promise<void> {
    const response = await safeFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
    if (!response.ok) throw httpError(response, {
      404: 'This task has already been deleted.',
      500: 'Server error while deleting the task — please try again.',
    });
  },
};
