<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { todoService } from '@/services/todoService';
import type { ToDo } from '@/types/todo';
import ToDoForm from '@/components/ToDoForm.vue';
import ToDoItem from '@/components/ToDoItem.vue';

const todos = ref<ToDo[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);
const saving = ref(false);

const showForm = ref(false);
const editingTodo = ref<ToDo | undefined>(undefined);
const pendingToggles = new Set<string>();

const fetchToDos = async () => {
  loading.value = true;
  error.value = null;
  try {
    todos.value = await todoService.getAll();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not load tasks — try refreshing the page.';
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

const remainingCount = computed(() => todos.value.filter(t => !t.finishedAt).length);

const handleSave = async (data: { title: string; description: string }) => {
  error.value = null;
  saving.value = true;
  try {
    if (editingTodo.value) {
      await todoService.update(editingTodo.value.id, data);
      const todo = todos.value.find(t => t.id === editingTodo.value!.id);
      if (todo) {
        todo.title = data.title;
        todo.description = data.description;
      }
    } else {
      const created = await todoService.create(data);
      todos.value.push(created);
    }
    showForm.value = false;
    editingTodo.value = undefined;
  } catch (err) {
    error.value = err instanceof Error
      ? err.message
      : editingTodo.value
        ? 'Could not save changes — please try again.'
        : 'Could not create the task — please try again.';
  } finally {
    saving.value = false;
  }
};

const handleToggleStatus = async (id: string, isCompleted: boolean) => {
  if (pendingToggles.has(id)) return;
  const todo = todos.value.find(t => t.id === id);
  if (!todo) return;
  error.value = null;
  const originalFinishedAt = todo.finishedAt;
  todo.finishedAt = isCompleted ? new Date().toISOString() : null;
  pendingToggles.add(id);
  try {
    await todoService.changeStatus(id, isCompleted);
  } catch (err) {
    todo.finishedAt = originalFinishedAt;
    error.value = err instanceof Error ? err.message : 'Could not update task status — please try again.';
  } finally {
    pendingToggles.delete(id);
  }
};

const handleDelete = async (id: string) => {
  error.value = null;
  try {
    await todoService.delete(id);
    todos.value = todos.value.filter(t => t.id !== id);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Could not delete the task — please try again.';
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
        <p class="subtitle">{{ remainingCount }} remaining</p>
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
        :submitting="saving"
        @save="handleSave"
        @cancel="cancelEdit"
      />
    </Transition>

    <div v-if="loading" class="skeleton-list" aria-label="Loading tasks...">
      <div v-for="n in 3" :key="n" class="skeleton-item">
        <div class="skeleton skeleton-checkbox"></div>
        <div class="skeleton-content">
          <div class="skeleton skeleton-title"></div>
          <div class="skeleton skeleton-desc"></div>
        </div>
      </div>
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

.empty-state {
  text-align: center;
  padding: 4rem 0;
  color: var(--text-color-light);
}

.skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.skeleton-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 1.25rem;
  background: var(--surface-color);
  border-radius: var(--border-radius);
  box-shadow: var(--box-shadow);
}

.skeleton {
  background: linear-gradient(90deg, var(--border-color) 25%, var(--surface-color) 50%, var(--border-color) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.4s infinite;
  border-radius: 4px;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.skeleton-checkbox {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
  border-radius: 6px;
}

.skeleton-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.skeleton-title {
  height: 1.125rem;
  width: 60%;
}

.skeleton-desc {
  height: 0.875rem;
  width: 40%;
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
