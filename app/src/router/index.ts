import { createRouter, createWebHistory, type Router } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import HomeView from '@/views/HomeView.vue'

export const routes = [
  {
    path: '/',
    name: 'home',
    component: HomeView,
    meta: { title: 'Home' }
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/LoginView.vue'),
    meta: { title: 'Login', noindex: true }
  },
  {
    path: '/admin',
    name: 'admin',
    component: () => import('@/views/AdminView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Admin Dashboard', noindex: true }
  },
  {
    path: '/admin/pages/create',
    name: 'page-create',
    component: () => import('@/views/PageCreateView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Create Page', noindex: true }
  },
  {
    path: '/admin/pages/:slug/edit',
    name: 'page-editor',
    component: () => import('@/views/PageEditorView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Edit Page', noindex: true }
  },
  {
    path: '/podcast',
    name: 'podcast',
    component: () => import('@/views/PodcastListView.vue'),
    meta: { title: 'Sermons' }
  },
  {
    path: '/sermon/:id',
    name: 'sermon-detail',
    component: () => import('@/views/SermonDetailView.vue'),
    meta: { title: 'Sermon' }
  },
  {
    path: '/admin/podcast',
    name: 'podcast-admin',
    component: () => import('@/views/PodcastAdminView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Sermon Admin', noindex: true }
  },
  {
    path: '/admin/podcast/create',
    name: 'podcast-create',
    component: () => import('@/views/PodcastCreateView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Create Sermon', noindex: true }
  },
  {
    path: '/admin/podcast/:id/edit',
    name: 'podcast-edit',
    component: () => import('@/views/PodcastEditView.vue'),
    meta: { requiresAuth: true, role: 'Admin', title: 'Edit Sermon', noindex: true }
  },
  {
    path: '/:slug',
    name: 'page',
    component: HomeView,
    meta: { title: 'Page' }
  }
]

const SITE_NAME = 'Brentwood Hills Primitive Baptist Church'

export function setupRouterGuards(router: Router) {
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

  router.afterEach((to) => {
    if (typeof document === 'undefined') return
    const title = to.meta.title as string | undefined
    if (title && title !== 'Home') {
      document.title = `${title} | ${SITE_NAME}`
    } else if (!title) {
      document.title = SITE_NAME
    }
  })
}

export function createAppRouter(): Router {
  const router = createRouter({
    history: createWebHistory(),
    routes
  })
  setupRouterGuards(router)
  return router
}

export default createAppRouter