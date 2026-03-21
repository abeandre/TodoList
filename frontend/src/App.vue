<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { authService } from '@/services/authService';
import { Routes } from '@/router/routes';

const router = useRouter();

const handleUnauthorized = () => {
  authService.clearUser();
  // Guard against NavigationDuplicated if already on the login page
  if (router.currentRoute.value.name !== Routes.Login) {
    router.push({ name: Routes.Login });
  }
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
