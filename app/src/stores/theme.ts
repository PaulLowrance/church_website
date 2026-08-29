import { defineStore } from 'pinia'
import { ref, watch } from 'vue'
import { Dark } from 'quasar'

export type Theme = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'theme-preference'

function getInitialTheme(): Theme {
  if (typeof localStorage === 'undefined') return 'system'
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark' || stored === 'system') return stored
  return 'system'
}

function isDarkTheme(theme: Theme): boolean {
  if (typeof window === 'undefined') return theme === 'dark'
  const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
  return theme === 'dark' || (theme === 'system' && systemDark)
}

function applyTheme(theme: Theme) {
  if (typeof document === 'undefined') return
  const root = document.documentElement
  const isDark = isDarkTheme(theme)

  root.setAttribute('data-theme', isDark ? 'dark' : 'light')
  Dark.set(isDark)
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
    // Toggle between light and dark. If currently on system, move to the opposite of the OS preference.
    if (theme.value === 'system') {
      theme.value = isDarkTheme('system') ? 'light' : 'dark'
    } else {
      theme.value = theme.value === 'dark' ? 'light' : 'dark'
    }
  }

  return { theme, setTheme, toggle }
})
