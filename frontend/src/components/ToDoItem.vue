<script setup lang="ts">
import type { ToDo } from '@/types/todo';

const props = defineProps<{
  todo: ToDo;
}>();

const emit = defineEmits<{
  (e: 'toggleStatus', id: string, isCompleted: boolean): void;
  (e: 'delete', id: string): void;
  (e: 'edit', todo: ToDo): void;
}>();

const formatDate = (dateStr: string) => {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  }).format(new Date(dateStr));
};
</script>

<template>
  <div class="todo-item" :class="{ 'is-completed': !!todo.finishedAt }">
    <div class="todo-checkbox">
      <label class="custom-checkbox">
        <input 
          type="checkbox" 
          :checked="!!todo.finishedAt" 
          @change="emit('toggleStatus', todo.id, ($event.target as HTMLInputElement).checked)"
        />
        <span class="checkmark"></span>
      </label>
    </div>
    
    <div class="todo-content">
      <h4 class="todo-title">{{ todo.title }}</h4>
      <p v-if="todo.description" class="todo-description">{{ todo.description }}</p>
      <span class="todo-date">Added {{ formatDate(todo.createdAt) }}</span>
    </div>

    <div class="todo-actions">
      <button class="action-btn edit-btn" @click="emit('edit', todo)" aria-label="Edit">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
      </button>
      <button class="action-btn delete-btn" @click="emit('delete', todo.id)" aria-label="Delete">
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line></svg>
      </button>
    </div>
  </div>
</template>

<style scoped>
.todo-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--surface-color);
  border-radius: var(--border-radius);
  box-shadow: var(--box-shadow);
  margin-bottom: 1rem;
  transition: all 0.3s ease;
  border: 1px solid transparent;
}

.todo-item:hover {
  box-shadow: var(--box-shadow-hover);
  transform: translateY(-2px);
  border-color: var(--border-color);
}

.todo-item.is-completed {
  opacity: 0.7;
  background: var(--surface-color-dim);
}

.todo-item.is-completed .todo-title {
  text-decoration: line-through;
  color: var(--text-color-light);
}

.todo-checkbox {
  padding-top: 0.25rem;
}

.custom-checkbox {
  display: block;
  position: relative;
  padding-left: 24px;
  cursor: pointer;
  user-select: none;
}

.custom-checkbox input {
  position: absolute;
  opacity: 0;
  cursor: pointer;
  height: 0;
  width: 0;
}

.checkmark {
  position: absolute;
  top: 0;
  left: 0;
  height: 24px;
  width: 24px;
  background-color: var(--background-color);
  border: 2px solid var(--border-color);
  border-radius: 6px;
  transition: all 0.2s ease;
}

.custom-checkbox:hover input ~ .checkmark {
  border-color: var(--primary-color);
}

.custom-checkbox input:checked ~ .checkmark {
  background-color: var(--success-color);
  border-color: var(--success-color);
}

.checkmark:after {
  content: "";
  position: absolute;
  display: none;
}

.custom-checkbox input:checked ~ .checkmark:after {
  display: block;
}

.custom-checkbox .checkmark:after {
  left: 7px;
  top: 3px;
  width: 6px;
  height: 12px;
  border: solid white;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}

.todo-content {
  flex: 1;
  min-width: 0;
}

.todo-title {
  margin: 0 0 0.5rem 0;
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--text-color);
  transition: color 0.3s ease;
}

.todo-description {
  margin: 0 0 0.75rem 0;
  font-size: 0.95rem;
  color: var(--text-color-light);
  line-height: 1.5;
}

.todo-date {
  font-size: 0.75rem;
  color: var(--text-color-lighter);
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.todo-actions {
  display: flex;
  gap: 0.5rem;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.todo-item:hover .todo-actions {
  opacity: 1;
}

.action-btn {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 6px;
  color: var(--text-color-light);
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-btn:hover {
  background: var(--background-color);
  transform: scale(1.05);
}

.edit-btn:hover {
  color: var(--primary-color);
}

.delete-btn:hover {
  color: var(--danger-color);
  background: rgba(239, 68, 68, 0.1);
}

@media (max-width: 640px) {
  .todo-actions {
    opacity: 1;
  }
}
</style>
