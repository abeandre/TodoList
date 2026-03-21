import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '@/views/HomeView.vue';
import LoginView from '@/views/LoginView.vue';
import RegisterView from '@/views/RegisterView.vue';
import { authService } from '@/services/authService';
import { Routes } from './routes';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: Routes.Home,
      component: HomeView,
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: Routes.Login,
      component: LoginView,
      meta: { guestOnly: true },
    },
    {
      path: '/register',
      name: Routes.Register,
      component: RegisterView,
      meta: { guestOnly: true },
    },
  ],
});

router.beforeEach((to, from, next) => {
  const isAuthenticated = authService.isAuthenticated();

  if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: Routes.Login });
  } else if (to.meta.guestOnly && isAuthenticated) {
    next({ name: Routes.Home });
  } else {
    next();
  }
});

export default router;
