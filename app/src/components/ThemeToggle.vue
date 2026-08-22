<script setup lang="ts">
import { computed } from 'vue'
import { useThemeStore } from '@/stores/theme'

const themeStore = useThemeStore()

const isDark = computed(() => themeStore.theme === 'dark')

const label = computed(() =>
  isDark.value ? 'Switch to light mode' : 'Switch to dark mode'
)
</script>

<template>
  <button
    type="button"
    class="theme-toggle"
    :aria-label="label"
    @click="themeStore.toggle"
  >
    <span class="theme-toggle__icon" aria-hidden="true">
      <svg v-if="isDark" class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="5" />
        <path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42" />
      </svg>
      <svg v-else class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
      </svg>
    </span>
    <span class="theme-toggle__text">{{ isDark ? 'Dark' : 'Light' }}</span>
  </button>
</template>

<style scoped>
.theme-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  padding: 0.375rem 0.75rem;
  border: 1px solid var(--rule);
  border-radius: 9999px;
  background: transparent;
  color: var(--ink);
  font-family: var(--sans);
  font-size: 0.875rem;
  line-height: 1;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.theme-toggle:hover {
  background: var(--rule);
}

.theme-toggle:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

.theme-toggle__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1rem;
  height: 1rem;
}

.icon {
  width: 100%;
  height: 100%;
}

.theme-toggle__text {
  text-transform: capitalize;
}
</style>
