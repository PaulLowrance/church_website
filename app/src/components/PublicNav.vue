<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import apiClient from '@/api/client'
import ThemeToggle from '@/components/ThemeToggle.vue'

interface NavItem {
  slug: string
  navTitle: string
}

const route = useRoute()
const navItems = ref<NavItem[]>([])
const menuOpen = ref(false)

onMounted(async () => {
  try {
    const response = await apiClient.get('/pages/nav')
    navItems.value = response.data
  } catch (error) {
    console.error('Failed to load nav items', error)
  }
})

function closeMenu() {
  menuOpen.value = false
}
</script>

<template>
  <header class="public-nav">
    <div class="public-nav__bar">
      <router-link to="/" class="public-nav__brand" aria-label="Brentwood Hills Primitive Baptist Church home">
        <img
          src="/uploads/images/bhpbc-header-footer.svg"
          alt="Brentwood Hills Primitive Baptist Church"
          class="public-nav__logo"
        />
      </router-link>

      <nav class="public-nav__desktop" aria-label="Main navigation">
        <router-link to="/" class="public-nav__link" :class="{ 'is-active': route.path === '/' }">
          Home
        </router-link>
        <router-link
          v-for="item in navItems"
          :key="item.slug"
          :to="`/${item.slug}`"
          class="public-nav__link"
          :class="{ 'is-active': route.params.slug === item.slug }"
        >
          {{ item.navTitle }}
        </router-link>
        <router-link
          to="/podcast"
          class="public-nav__link"
          :class="{ 'is-active': route.path === '/podcast' }"
        >
          Sermons
        </router-link>
      </nav>

      <div class="public-nav__actions">
        <ThemeToggle />
        <button
          type="button"
          class="public-nav__toggle"
          :aria-expanded="menuOpen"
          aria-label="Toggle navigation menu"
          @click="menuOpen = !menuOpen"
        >
          <span class="public-nav__toggle-bar" />
          <span class="public-nav__toggle-bar" />
          <span class="public-nav__toggle-bar" />
        </button>
      </div>
    </div>

    <nav
      v-if="menuOpen"
      class="public-nav__mobile"
      aria-label="Mobile navigation"
      @click="closeMenu"
    >
      <router-link to="/" class="public-nav__mobile-link">Home</router-link>
      <router-link
        v-for="item in navItems"
        :key="item.slug"
        :to="`/${item.slug}`"
        class="public-nav__mobile-link"
      >
        {{ item.navTitle }}
      </router-link>
      <router-link to="/podcast" class="public-nav__mobile-link">Sermons</router-link>
    </nav>
  </header>
</template>

<style scoped>
.public-nav {
  position: sticky;
  top: 0;
  z-index: 50;
  background: var(--brand-burgundy);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.18);
}

.public-nav__bar {
  display: flex;
  align-items: center;
  width: 100%;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-3);
}

.public-nav__brand {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;
  text-decoration: none;
}

.public-nav__logo {
  height: 38px;
  width: auto;
  max-width: 220px;
  object-fit: contain;
  object-position: left;
  display: block;
  filter: brightness(0) invert(1);
}

.public-nav__desktop {
  display: none;
  flex: 1;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  min-width: 0;
  overflow: hidden;
}

@media (min-width: 768px) {
  .public-nav__desktop {
    display: flex;
  }
}

.public-nav__link {
  font-family: var(--sans);
  font-size: 0.9375rem;
  font-weight: 500;
  color: #fff;
  text-decoration: none;
  white-space: nowrap;
  padding: 0.375rem 0.5rem;
  border-radius: 6px;
  transition: background-color 0.2s ease, color 0.2s ease;
}

.public-nav__link:hover {
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
}

.public-nav__link.is-active {
  background: #fff;
  color: var(--ink, #1c1a17);
}

.public-nav__actions {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  flex-shrink: 0;
  --toggle-border: rgba(255, 255, 255, 0.5);
  --toggle-color: #fff;
  --toggle-hover: rgba(255, 255, 255, 0.15);
}

.public-nav__toggle {
  display: inline-flex;
  flex-direction: column;
  gap: 4px;
  padding: 0.5rem;
  border: 1px solid rgba(255, 255, 255, 0.5);
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
}

@media (min-width: 768px) {
  .public-nav__toggle {
    display: none;
  }
}

.public-nav__toggle-bar {
  width: 20px;
  height: 2px;
  background: #fff;
  border-radius: 2px;
}

.public-nav__mobile {
  display: flex;
  flex-direction: column;
  gap: 0;
  max-width: 1200px;
  margin: 0 auto;
  padding: var(--space-2) var(--space-4) var(--space-3);
  background: var(--paper);
  border-radius: 0 0 12px 12px;
  box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
}

@media (min-width: 768px) {
  .public-nav__mobile {
    display: none;
  }
}

.public-nav__mobile-link {
  font-family: var(--sans);
  font-size: 1rem;
  color: var(--ink-soft);
  text-decoration: none;
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--rule);
}

.public-nav__mobile-link:hover {
  color: var(--accent-burgundy);
}
</style>