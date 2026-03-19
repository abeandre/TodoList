import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ToDoItem from '../ToDoItem.vue';
import type { ToDo } from '@/types/todo';

describe('ToDoItem.vue', () => {
  const mockValidDate = new Date().toISOString();
  
  const mockTodo: ToDo = {
    id: 'test-1',
    title: 'Test Title',
    description: 'Test Describe',
    finishedAt: null,
    createdAt: mockValidDate
  };

  it('renders a ToDo correctly', () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });
    
    expect(wrapper.find('.todo-title').text()).toBe('Test Title');
    expect(wrapper.find('.todo-description').text()).toBe('Test Describe');
    expect(wrapper.find('.todo-item').classes()).not.toContain('is-completed');
    expect((wrapper.find('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(false);
  });

  it('renders a completed ToDo correctly', () => {
    const completedTodo = { ...mockTodo, finishedAt: new Date().toISOString() };
    const wrapper = mount(ToDoItem, {
      props: { todo: completedTodo }
    });
    
    expect(wrapper.find('.todo-item').classes()).toContain('is-completed');
    expect((wrapper.find('input[type="checkbox"]').element as HTMLInputElement).checked).toBe(true);
  });

  it('emits toggleStatus when checkbox is clicked', async () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });
    
    const checkbox = wrapper.find('input[type="checkbox"]');
    await checkbox.setValue(true); // Simulate checking it
    
    expect(wrapper.emitted('toggleStatus')).toBeTruthy();
    expect(wrapper.emitted('toggleStatus')?.[0]).toEqual(['test-1', true]);
  });

  it('emits edit when edit button is clicked', async () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });
    
    await wrapper.find('.edit-btn').trigger('click');
    
    expect(wrapper.emitted('edit')).toBeTruthy();
    expect(wrapper.emitted('edit')?.[0]).toEqual([mockTodo]);
  });

  it('emits delete when delete button is clicked', async () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });
    
    await wrapper.find('.delete-btn').trigger('click');
    
    expect(wrapper.emitted('delete')).toBeTruthy();
    expect(wrapper.emitted('delete')?.[0]).toEqual(['test-1']);
  });
});
