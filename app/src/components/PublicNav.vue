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
  background: var(--paper);
  border-bottom: 1px solid var(--rule);
}

.public-nav__bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-4);
  max-width: 1200px;
  margin: 0 auto;
  padding: var(--space-3) var(--space-4);
}

.public-nav__brand {
  display: inline-flex;
  align-items: center;
  text-decoration: none;
}

.public-nav__logo {
  height: 44px;
  width: auto;
  display: block;
}

.public-nav__desktop {
  display: none;
  align-items: center;
  gap: var(--space-5);
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
  color: var(--ink-soft);
  text-decoration: none;
  padding: 0.25rem 0;
  border-bottom: 2px solid transparent;
  transition: color 0.2s ease, border-color 0.2s ease;
}

.public-nav__link:hover {
  color: var(--accent-burgundy);
}

.public-nav__link.is-active {
  color: var(--accent-burgundy);
  border-bottom-color: var(--accent-gold);
}

.public-nav__actions {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.public-nav__toggle {
  display: inline-flex;
  flex-direction: column;
  gap: 4px;
  padding: 0.5rem;
  border: 1px solid var(--rule);
  border-radius: 8px;
  background: var(--panel);
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
  background: var(--ink);
  border-radius: 2px;
}

.public-nav__mobile {
  display: flex;
  flex-direction: column;
  gap: 0;
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 var(--space-4) var(--space-3);
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