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
    createdAt: mockValidDate,
    updatedAt: mockValidDate
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

  it('shows confirmation on delete button click, then emits delete on confirm', async () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });

    // First click shows confirmation
    await wrapper.find('.delete-btn').trigger('click');
    expect(wrapper.find('.confirm-yes-btn').exists()).toBe(true);
    expect(wrapper.emitted('delete')).toBeFalsy();

    // Second click (confirm) emits delete
    await wrapper.find('.confirm-yes-btn').trigger('click');
    expect(wrapper.emitted('delete')).toBeTruthy();
    expect(wrapper.emitted('delete')?.[0]).toEqual(['test-1']);
  });

  it('cancels delete when No is clicked', async () => {
    const wrapper = mount(ToDoItem, {
      props: { todo: mockTodo }
    });

    await wrapper.find('.delete-btn').trigger('click');
    await wrapper.find('.confirm-no-btn').trigger('click');

    expect(wrapper.find('.delete-btn').exists()).toBe(true);
    expect(wrapper.emitted('delete')).toBeFalsy();
  });
});
