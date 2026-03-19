<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { todoService } from '@/services/todoService';
import type { ToDo } from '@/types/todo';
import ToDoForm from '@/components/ToDoForm.vue';
import ToDoItem from '@/components/ToDoItem.vue';

const todos = ref<ToDo[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);

const showForm = ref(false);
const editingTodo = ref<ToDo | undefined>(undefined);

const fetchToDos = async () => {
  loading.value = true;
  error.value = null;
  try {
    todos.value = await todoService.getAll();
  } catch (err: any) {
    error.value = err.message || 'Failed to load ToDos';
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchToDos();
});

const sortedToDos = computed(() => {
  return [...todos.value].sort((a, b) => {
    // Show active items first
    const aCompleted = !!a.finishedAt;
    const bCompleted = !!b.finishedAt;
    
    if (aCompleted !== bCompleted) {
      return aCompleted ? 1 : -1;
    }
    // Then sort by date descending
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });
});

const handleSave = async (data: { title: string; description: string }) => {
  try {
    if (editingTodo.value) {
      await todoService.update(editingTodo.value.id, { id: editingTodo.value.id, ...data });
    } else {
      await todoService.create(data);
    }
    await fetchToDos();
    showForm.value = false;
    editingTodo.value = undefined;
  } catch (err: any) {
    error.value = err.message || 'Failed to save ToDo';
  }
};

const handleToggleStatus = async (id: string, isCompleted: boolean) => {
  try {
    // Optimistic UI update
    const todo = todos.value.find(t => t.id === id);
    if (todo) todo.finishedAt = isCompleted ? new Date().toISOString() : null;
    
    await todoService.changeStatus(id, isCompleted);
    // Fetch fresh to get accurate dates if needed
    // await fetchToDos();
  } catch (err: any) {
    // Revert on failure
    const todo = todos.value.find(t => t.id === id);
    if (todo) todo.finishedAt = !isCompleted ? new Date().toISOString() : null;
    error.value = err.message || 'Failed to update status';
  }
};

const handleDelete = async (id: string) => {
  try {
    await todoService.delete(id);
    todos.value = todos.value.filter(t => t.id !== id);
  } catch (err: any) {
    error.value = err.message || 'Failed to delete ToDo';
  }
};

const startEdit = (todo: ToDo) => {
  editingTodo.value = todo;
  showForm.value = true;
};

const cancelEdit = () => {
  editingTodo.value = undefined;
  showForm.value = false;
};
</script>

<template>
  <main class="app-container">
    <header class="app-header">
      <div class="header-titles">
        <h1>My Tasks</h1>
        <p class="subtitle">{{ todos.filter(t => !t.finishedAt).length }} remaining</p>
      </div>
      <button v-if="!showForm" class="btn btn-primary new-task-btn" @click="showForm = true">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
        New Task
      </button>
    </header>

    <div v-if="error" class="error-banner">
      {{ error }}
      <button @click="error = null" class="close-btn">&times;</button>
    </div>

    <Transition name="slide-fade">
      <ToDoForm 
        v-if="showForm" 
        :todo="editingTodo" 
        @save="handleSave" 
        @cancel="cancelEdit" 
      />
    </Transition>

    <div v-if="loading" class="loading-state">
      <div class="spinner"></div>
      <p>Loading your tasks...</p>
    </div>

    <div v-else-if="todos.length === 0" class="empty-state">
      <div class="empty-icon">📝</div>
      <h3>Nothing to do</h3>
      <p>Clean slate! Add some tasks to get started.</p>
    </div>

    <TransitionGroup v-else name="list" tag="div" class="todo-list">
      <ToDoItem 
        v-for="todo in sortedToDos" 
        :key="todo.id" 
        :todo="todo" 
        @toggle-status="handleToggleStatus"
        @delete="handleDelete"
        @edit="startEdit"
      />
    </TransitionGroup>
  </main>
</template>

<style scoped>
.app-container {
  max-width: 48rem;
  margin: 0 auto;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid var(--border-color);
}

.header-titles h1 {
  font-size: 2.5rem;
  font-weight: 800;
  margin: 0 0 0.25rem 0;
  background: linear-gradient(to right, #6366f1, #a855f7);
  -webkit-background-clip: text;
  color: transparent;
  letter-spacing: -0.025em;
}

.subtitle {
  margin: 0;
  color: var(--text-color-light);
  font-weight: 500;
}

.new-task-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.25rem;
  font-weight: 600;
  border-radius: var(--border-radius);
  background: var(--primary-color);
  color: white;
  border: none;
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 6px -1px rgba(99, 102, 241, 0.3);
}

.new-task-btn:hover {
  background: var(--primary-hover);
  transform: translateY(-2px);
  box-shadow: 0 6px 8px -1px rgba(99, 102, 241, 0.4);
}

.error-banner {
  background: rgba(239, 68, 68, 0.1);
  color: var(--danger-color);
  border: 1px solid rgba(239, 68, 68, 0.2);
  padding: 1rem;
  border-radius: var(--border-radius);
  margin-bottom: 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.close-btn {
  background: none;
  border: none;
  color: inherit;
  font-size: 1.5rem;
  cursor: pointer;
}

.loading-state, .empty-state {
  text-align: center;
  padding: 4rem 0;
  color: var(--text-color-light);
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--border-color);
  border-top-color: var(--primary-color);
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.empty-icon {
  font-size: 3rem;
  margin-bottom: 1rem;
  filter: grayscale(1) opacity(0.5);
}

.empty-state h3 {
  color: var(--text-color);
  margin: 0 0 0.5rem 0;
  font-size: 1.25rem;
}

/* Transitions */
.list-enter-active,
.list-leave-active {
  transition: all 0.4s ease;
}
.list-enter-from {
  opacity: 0;
  transform: translateX(-30px);
}
.list-leave-to {
  opacity: 0;
  transform: translateX(30px);
}

.slide-fade-enter-active {
  transition: all 0.3s ease-out;
}
.slide-fade-leave-active {
  transition: all 0.2s cubic-bezier(1, 0.5, 0.8, 1);
}
.slide-fade-enter-from,
.slide-fade-leave-to {
  transform: translateY(-20px);
  opacity: 0;
}
</style>
