<script setup lang="ts">
import { ref, watch } from 'vue';
import type { ToDo } from '@/types/todo';

const props = defineProps<{
  todo?: ToDo;
}>();

const emit = defineEmits<{
  (e: 'save', data: { title: string; description: string }): void;
  (e: 'cancel'): void;
}>();

const title = ref(props.todo?.title ?? '');
const description = ref(props.todo?.description ?? '');

watch(() => props.todo, (newVal) => {
  title.value = newVal?.title ?? '';
  description.value = newVal?.description ?? '';
});

const submit = () => {
  if (!title.value.trim()) return;
  emit('save', { title: title.value, description: description.value });
  if (!props.todo) {
    title.value = '';
    description.value = '';
  }
};
</script>

<template>
  <div class="todo-form">
    <h3 class="form-title">{{ todo ? 'Edit ToDo' : 'Add New ToDo' }}</h3>
    <form @submit.prevent="submit" class="form-content">
      <div class="form-group">
        <label for="title">Title</label>
        <input 
          id="title" 
          v-model="title" 
          type="text" 
          required 
          placeholder="What needs to be done?"
          class="form-input"
        />
      </div>
      
      <div class="form-group">
        <label for="description">Description</label>
        <textarea 
          id="description" 
          v-model="description" 
          rows="3" 
          placeholder="Add some details..."
          class="form-input textarea"
        ></textarea>
      </div>

      <div class="form-actions">
        <button type="button" v-if="todo" @click="emit('cancel')" class="btn btn-secondary">Cancel</button>
        <button type="submit" class="btn btn-primary">
          {{ todo ? 'Save Changes' : 'Add ToDo' }}
        </button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.todo-form {
  background: var(--surface-color);
  border-radius: var(--border-radius);
  padding: 1.5rem;
  box-shadow: var(--box-shadow);
  margin-bottom: 2rem;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.todo-form:hover {
  box-shadow: var(--box-shadow-hover);
}

.form-title {
  margin-top: 0;
  margin-bottom: 1.5rem;
  color: var(--text-color);
  font-size: 1.25rem;
  font-weight: 600;
}

.form-content {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-color-light);
}

.form-input {
  padding: 0.75rem 1rem;
  border: 1px solid var(--border-color);
  border-radius: calc(var(--border-radius) / 2);
  background: var(--background-color);
  color: var(--text-color);
  font-family: inherit;
  font-size: 1rem;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.form-input:focus {
  outline: none;
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.2);
}

.textarea {
  resize: vertical;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 0.5rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: calc(var(--border-radius) / 2);
  font-weight: 600;
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-primary {
  background: var(--primary-color);
  color: white;
}

.btn-primary:hover {
  background: var(--primary-hover);
  transform: translateY(-1px);
}

.btn-secondary {
  background: var(--secondary-color);
  color: var(--text-color);
}

.btn-secondary:hover {
  background: var(--border-color);
}
</style>
