import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import App from '../App.vue';
import { todoService } from '@/services/todoService';

vi.mock('@/services/todoService', () => ({
  todoService: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    changeStatus: vi.fn(),
    delete: vi.fn()
  }
}));

describe('App.vue (ToDo List)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', async () => {
    (todoService.getAll as any).mockImplementation(() => new Promise(resolve => setTimeout(() => resolve([]), 100)));
    const wrapper = mount(App);
    
    expect(wrapper.find('.skeleton-list').exists()).toBe(true);
  });

  it('renders empty state when no ToDos', async () => {
    (todoService.getAll as any).mockResolvedValue([]);
    const wrapper = mount(App);
    
    // Wait for the promise to resolve and DOM to update
    await flushPromises();
    
    expect(wrapper.find('.empty-state').exists()).toBe(true);
    expect(wrapper.text()).toContain('Nothing to do');
  });

  it('renders a list of ToDos fetching from API', async () => {
    const mockToDos = [
      { id: '1', title: 'Task 1', description: '', finishedAt: null, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() },
      { id: '2', title: 'Task 2', description: '', finishedAt: new Date().toISOString(), createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
    ];
    (todoService.getAll as any).mockResolvedValue(mockToDos);
    const wrapper = mount(App);
    
    await flushPromises();
    
    expect(wrapper.findAllComponents({ name: 'ToDoItem' }).length).toBe(2);
    expect(wrapper.find('.subtitle').text()).toBe('1 remaining');
  });
});
