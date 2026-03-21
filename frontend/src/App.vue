<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { authService } from '@/services/authService';

const router = useRouter();

const handleUnauthorized = () => {
  authService.clearToken();
  router.push('/login');
};

onMounted(() => {
  window.addEventListener('unauthorized-error', handleUnauthorized);
});

onUnmounted(() => {
  window.removeEventListener('unauthorized-error', handleUnauthorized);
});
</script>

<template>
  <router-view />
</template>
