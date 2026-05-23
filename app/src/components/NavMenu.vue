<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

interface NavItem {
  slug: string
  navTitle: string
}

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const navItems = ref<NavItem[]>([])
const drawerOpen = ref(false)

const currentRouteName = computed(() => route.name as string)

onMounted(async () => {
  try {
    const response = await apiClient.get('/pages/nav')
    navItems.value = response.data
  } catch (error) {
    console.error('Failed to load nav items', error)
  }
})

function goHome() {
  drawerOpen.value = false
  router.push('/')
}

function goToPage(slug: string) {
  drawerOpen.value = false
  router.push(`/${slug}`)
}

function goAdmin() {
  drawerOpen.value = false
  router.push('/admin')
}

function goLogin() {
  drawerOpen.value = false
  router.push('/login')
}

function logout() {
  drawerOpen.value = false
  authStore.logout()
  router.push('/')
}
</script>

<template>
  <q-header elevated class="bg-primary text-white">
    <q-toolbar>
      <q-btn
        dense
        flat
        round
        icon="menu"
        aria-label="Toggle navigation menu"
        @click="drawerOpen = !drawerOpen"
        class="lt-md"
      />
      <q-toolbar-title>
        <q-btn flat dense class="text-white" @click="goHome" aria-label="Go to home page">
          Church Website
        </q-btn>
      </q-toolbar-title>

      <!-- Desktop nav -->
      <nav class="gt-sm" role="navigation" aria-label="Main navigation">
        <q-btn
          flat
          dense
          class="text-white"
          :class="{ 'bg-white text-primary': currentRouteName === 'home' }"
          @click="goHome"
          aria-label="Home"
        >
          Home
        </q-btn>
        <q-btn
          v-for="item in navItems"
          :key="item.slug"
          flat
          dense
          class="text-white q-ml-sm"
          :class="{ 'bg-white text-primary': route.params.slug === item.slug }"
          @click="goToPage(item.slug)"
          :aria-label="item.navTitle"
          :aria-current="route.params.slug === item.slug ? 'page' : undefined"
        >
          {{ item.navTitle }}
        </q-btn>
        <q-btn
          v-if="authStore.userRole === 'Admin'"
          flat
          dense
          class="text-white q-ml-sm"
          :class="{ 'bg-white text-primary': currentRouteName === 'admin' }"
          @click="goAdmin"
          aria-label="Admin Dashboard"
        >
          Admin
        </q-btn>
        <q-btn
          v-if="authStore.isAuthenticated"
          flat
          dense
          class="text-white q-ml-sm"
          @click="logout"
          aria-label="Log out"
        >
          Logout
        </q-btn>
        <q-btn
          v-else
          flat
          dense
          class="text-white q-ml-sm"
          :class="{ 'bg-white text-primary': currentRouteName === 'login' }"
          @click="goLogin"
          aria-label="Log in"
        >
          Login
        </q-btn>
      </nav>
    </q-toolbar>
  </q-header>

  <!-- Mobile drawer -->
  <q-drawer
    v-model="drawerOpen"
    side="left"
    bordered
    behavior="mobile"
    :aria-hidden="!drawerOpen"
  >
    <q-list role="menu" aria-label="Mobile navigation menu">
      <q-item-label header>Navigation</q-item-label>
      <q-item
        clickable
        v-ripple
        @click="goHome"
        :active="currentRouteName === 'home'"
        role="menuitem"
        aria-label="Home"
      >
        <q-item-section>Home</q-item-section>
      </q-item>
      <q-item
        v-for="item in navItems"
        :key="item.slug"
        clickable
        v-ripple
        @click="goToPage(item.slug)"
        :active="route.params.slug === item.slug"
        role="menuitem"
        :aria-label="item.navTitle"
      >
        <q-item-section>{{ item.navTitle }}</q-item-section>
      </q-item>
      <q-separator />
      <q-item
        v-if="authStore.userRole === 'Admin'"
        clickable
        v-ripple
        @click="goAdmin"
        :active="currentRouteName === 'admin'"
        role="menuitem"
        aria-label="Admin Dashboard"
      >
        <q-item-section>Admin</q-item-section>
      </q-item>
      <q-item
        v-if="authStore.isAuthenticated"
        clickable
        v-ripple
        @click="logout"
        role="menuitem"
        aria-label="Log out"
      >
        <q-item-section>Logout</q-item-section>
      </q-item>
      <q-item
        v-else
        clickable
        v-ripple
        @click="goLogin"
        :active="currentRouteName === 'login'"
        role="menuitem"
        aria-label="Log in"
      >
        <q-item-section>Login</q-item-section>
      </q-item>
    </q-list>
  </q-drawer>
</template>
