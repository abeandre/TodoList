import type { ToDo } from '@/types/todo';

const API_BASE_URL = '/api/todo';

export const todoService = {
  async getAll(): Promise<ToDo[]> {
    const response = await fetch(API_BASE_URL);
    if (!response.ok) throw new Error('Failed to fetch ToDos');
    return response.json();
  },

  async getById(id: string): Promise<ToDo> {
    const response = await fetch(`${API_BASE_URL}/${id}`);
    if (!response.ok) throw new Error(`Failed to fetch ToDo ${id}`);
    return response.json();
  },

  async create(todo: Pick<ToDo, 'title' | 'description'>): Promise<ToDo> {
    const response = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(todo)
    });
    if (!response.ok) throw new Error('Failed to create ToDo');
    return response.json();
  },

  async update(id: string, todo: Pick<ToDo, 'title' | 'description' | 'id'>): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(todo)
    });
    if (!response.ok) throw new Error(`Failed to update ToDo ${id}`);
  },

  async changeStatus(id: string, isCompleted: boolean): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/${id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(isCompleted)
    });
    if (!response.ok) throw new Error(`Failed to change status of ToDo ${id}`);
  },

  async delete(id: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE'
    });
    if (!response.ok) throw new Error(`Failed to delete ToDo ${id}`);
  }
};
