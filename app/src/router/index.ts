import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import HomeView from '@/views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue')
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('@/views/AdminView.vue'),
      meta: { requiresAuth: true, role: 'Admin' }
    },
    {
      path: '/admin/pages/create',
      name: 'page-create',
      component: () => import('@/views/PageCreateView.vue'),
      meta: { requiresAuth: true, role: 'Admin' }
    },
    {
      path: '/admin/pages/:slug/edit',
      name: 'page-editor',
      component: () => import('@/views/PageEditorView.vue'),
      meta: { requiresAuth: true, role: 'Admin' }
    },
    {
      path: '/:slug',
      name: 'page',
      component: HomeView
    }
  ]
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.meta.role && authStore.userRole !== to.meta.role) {
    next('/')
  } else {
    next()
  }
})

export default router
