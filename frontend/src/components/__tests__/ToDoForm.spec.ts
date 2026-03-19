import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ToDoForm from '../ToDoForm.vue';
import type { ToDo } from '@/types/todo';

describe('ToDoForm.vue', () => {
  it('renders correctly for a new ToDo', () => {
    const wrapper = mount(ToDoForm);
    expect(wrapper.find('.form-title').text()).toBe('Add New ToDo');
    expect(wrapper.find('button[type="submit"]').text()).toBe('Add ToDo');
    expect(wrapper.find('button.btn-secondary').exists()).toBe(false);
  });

  it('renders correctly for editing an existing ToDo', () => {
    const mockToDo: ToDo = {
      id: '123',
      title: 'Current Task',
      description: 'Current Desc',
      finishedAt: null,
      createdAt: new Date().toISOString()
    };
    
    const wrapper = mount(ToDoForm, {
      props: { todo: mockToDo }
    });

    expect(wrapper.find('.form-title').text()).toBe('Edit ToDo');
    expect((wrapper.find('#title').element as HTMLInputElement).value).toBe('Current Task');
    expect((wrapper.find('#description').element as HTMLTextAreaElement).value).toBe('Current Desc');
    expect(wrapper.find('button[type="submit"]').text()).toBe('Save Changes');
    expect(wrapper.find('button.btn-secondary').exists()).toBe(true);
  });

  it('emits save event with correct payload on submit', async () => {
    const wrapper = mount(ToDoForm);
    
    // Set values
    await wrapper.find('#title').setValue('New Task Title');
    await wrapper.find('#description').setValue('New Description');
    
    // Submit form
    await wrapper.find('form').trigger('submit.prevent');
    
    // Check emitted events
    expect(wrapper.emitted('save')).toBeTruthy();
    expect(wrapper.emitted('save')?.[0]).toEqual([{
      title: 'New Task Title',
      description: 'New Description'
    }]);
  });

  it('clears form after adding new ToDo', async () => {
    const wrapper = mount(ToDoForm);
    
    await wrapper.find('#title').setValue('Task to clear');
    await wrapper.find('#description').setValue('Should be cleared');
    await wrapper.find('form').trigger('submit.prevent');
    
    expect((wrapper.find('#title').element as HTMLInputElement).value).toBe('');
    expect((wrapper.find('#description').element as HTMLTextAreaElement).value).toBe('');
  });

  it('does not emit save if title is empty', async () => {
    const wrapper = mount(ToDoForm);
    
    await wrapper.find('#title').setValue('   '); // whitespace only
    await wrapper.find('form').trigger('submit.prevent');
    
    expect(wrapper.emitted('save')).toBeFalsy();
  });

  it('emits cancel event when editing and clicking Cancel', async () => {
    const mockToDo: ToDo = {
      id: '123',
      title: 'Task',
      description: '',
      finishedAt: null,
      createdAt: new Date().toISOString()
    };
    
    const wrapper = mount(ToDoForm, {
      props: { todo: mockToDo }
    });
    
    await wrapper.find('button.btn-secondary').trigger('click');
    expect(wrapper.emitted('cancel')).toBeTruthy();
  });
});
