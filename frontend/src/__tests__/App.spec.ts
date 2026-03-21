import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import HomeView from '../views/HomeView.vue';
import { todoService } from '@/services/todoService';
import { authService } from '@/services/authService';
import type { ToDo, ISODateTime } from '@/types/todo';

vi.mock('@/services/todoService', () => ({
  todoService: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    changeStatus: vi.fn(),
    delete: vi.fn()
  }
}));

vi.mock('@/services/authService', () => ({
  authService: {
    logout: vi.fn(),
    getUserId: vi.fn().mockReturnValue('user-123'),
    deleteAccount: vi.fn(),
    isAuthenticated: vi.fn().mockReturnValue(true),
    clearUser: vi.fn(),
  }
}));

const mockPush = vi.fn();
vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mockPush }),
}));

const now = new Date().toISOString() as ISODateTime;

const makeTodo = (overrides: Partial<ToDo> = {}): ToDo => ({
  id: '1',
  title: 'Task',
  description: '',
  finishedAt: null,
  createdAt: now,
  updatedAt: now,
  ...overrides
});

describe('HomeView.vue (ToDo List)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (authService.getUserId as any).mockReturnValue('user-123');
    (authService.isAuthenticated as any).mockReturnValue(true);
  });

  it('renders loading state initially', async () => {
    (todoService.getAll as any).mockImplementation(() => new Promise(resolve => setTimeout(() => resolve([]), 100)));
    const wrapper = mount(HomeView);
    expect(wrapper.find('.skeleton-list').exists()).toBe(true);
  });

  it('renders empty state when no ToDos', async () => {
    (todoService.getAll as any).mockResolvedValue([]);
    const wrapper = mount(HomeView);
    await flushPromises();
    expect(wrapper.find('.empty-state').exists()).toBe(true);
    expect(wrapper.text()).toContain('Nothing to do');
  });

  it('renders a list of ToDos fetching from API', async () => {
    const mockToDos: ToDo[] = [
      makeTodo({ id: '1', title: 'Task 1', finishedAt: null }),
      makeTodo({ id: '2', title: 'Task 2', finishedAt: now })
    ];
    (todoService.getAll as any).mockResolvedValue(mockToDos);
    const wrapper = mount(HomeView);
    await flushPromises();
    expect(wrapper.findAllComponents({ name: 'ToDoItem' }).length).toBe(2);
    expect(wrapper.find('.subtitle').text()).toBe('1 remaining');
  });

  it('shows error banner when API fetch fails', async () => {
    (todoService.getAll as any).mockRejectedValue(new Error('Network error'));
    const wrapper = mount(HomeView);
    await flushPromises();
    expect(wrapper.find('.error-banner').exists()).toBe(true);
    expect(wrapper.find('.error-banner').text()).toContain('Network error');
  });

  it('creates a new todo and adds it to the list', async () => {
    (todoService.getAll as any).mockResolvedValue([]);
    const created = makeTodo({ id: 'new-1', title: 'New Task' });
    (todoService.create as any).mockResolvedValue(created);
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('.new-task-btn').trigger('click');
    await wrapper.find('#title').setValue('New Task');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(todoService.create).toHaveBeenCalledWith({ title: 'New Task', description: '' });
    expect(wrapper.findAllComponents({ name: 'ToDoItem' }).length).toBe(1);
  });

  it('shows error banner when create fails', async () => {
    (todoService.getAll as any).mockResolvedValue([]);
    (todoService.create as any).mockRejectedValue(new Error('Create failed'));
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('.new-task-btn').trigger('click');
    await wrapper.find('#title').setValue('Will fail');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.find('.error-banner').exists()).toBe(true);
  });

  it('edits a todo and updates its title in the list', async () => {
    const todo = makeTodo({ id: '1', title: 'Original' });
    (todoService.getAll as any).mockResolvedValue([todo]);
    (todoService.update as any).mockResolvedValue(undefined);
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('.edit-btn').trigger('click');
    await wrapper.find('#title').setValue('Updated Title');
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(todoService.update).toHaveBeenCalledWith('1', { title: 'Updated Title', description: '' });
    expect(wrapper.find('.todo-title').text()).toBe('Updated Title');
  });

  it('deletes a todo and removes it from the list', async () => {
    const todo = makeTodo({ id: '1', title: 'To Delete' });
    (todoService.getAll as any).mockResolvedValue([todo]);
    (todoService.delete as any).mockResolvedValue(undefined);
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('.delete-btn').trigger('click');
    await wrapper.find('.confirm-yes-btn').trigger('click');
    await flushPromises();

    expect(todoService.delete).toHaveBeenCalledWith('1');
    expect(wrapper.findAllComponents({ name: 'ToDoItem' }).length).toBe(0);
  });

  it('shows error banner when delete fails', async () => {
    const todo = makeTodo({ id: '1', title: 'Will fail delete' });
    (todoService.getAll as any).mockResolvedValue([todo]);
    (todoService.delete as any).mockRejectedValue(new Error('Delete failed'));
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('.delete-btn').trigger('click');
    await wrapper.find('.confirm-yes-btn').trigger('click');
    await flushPromises();

    expect(wrapper.find('.error-banner').exists()).toBe(true);
  });

  it('updates finishedAt only after API confirms status change', async () => {
    const todo = makeTodo({ id: '1' });
    (todoService.getAll as any).mockResolvedValue([todo]);
    let resolve!: () => void;
    (todoService.changeStatus as any).mockImplementation(() => new Promise<void>(r => { resolve = r; }));
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('input[type="checkbox"]').setValue(true);

    expect(wrapper.find('.todo-item').classes()).not.toContain('is-completed');

    resolve();
    await flushPromises();

    expect(wrapper.find('.todo-item').classes()).toContain('is-completed');
  });

  it('shows error banner when status change fails', async () => {
    const todo = makeTodo({ id: '1' });
    (todoService.getAll as any).mockResolvedValue([todo]);
    (todoService.changeStatus as any).mockRejectedValue(new Error('Status change failed'));
    const wrapper = mount(HomeView);
    await flushPromises();

    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    expect(wrapper.find('.error-banner').exists()).toBe(true);
    expect(wrapper.find('.todo-item').classes()).not.toContain('is-completed');
  });
});
