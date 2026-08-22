import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export type Theme = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'theme-preference'

function getInitialTheme(): Theme {
  if (typeof localStorage === 'undefined') return 'system'
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark' || stored === 'system') return stored
  return 'system'
}

function applyTheme(theme: Theme) {
  const root = document.documentElement
  const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
  const isDark = theme === 'dark' || (theme === 'system' && systemDark)

  if (isDark) {
    root.setAttribute('data-theme', 'dark')
  } else {
    root.setAttribute('data-theme', 'light')
  }
}

export const useThemeStore = defineStore('theme', () => {
  const theme = ref<Theme>(getInitialTheme())

  applyTheme(theme.value)

  watch(theme, (newTheme) => {
    localStorage.setItem(STORAGE_KEY, newTheme)
    applyTheme(newTheme)
  })

  function setTheme(newTheme: Theme) {
    theme.value = newTheme
  }

  function toggle() {
    const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    const currentIsDark = theme.value === 'dark' || (theme.value === 'system' && systemDark)
    theme.value = currentIsDark ? 'light' : 'dark'
  }

  return { theme, setTheme, toggle }
})
