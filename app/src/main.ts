import { ViteSSG } from 'vite-ssg'
import { createPinia } from 'pinia'
import { createHead } from '@unhead/vue/client'
import { Quasar, Dark } from 'quasar'
import quasarLang from 'quasar/lang/en-US.js'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/src/css/index.sass'
import './style.css'
import App from './App.vue'
import { routes, setupRouterGuards } from './router'
import { useThemeStore } from '@/stores/theme'

export const createApp = ViteSSG(
  App,
  { routes },
  ({ app, router, isClient }) => {
    app.use(createPinia())
    app.use(createHead())

    const quasarOptions = {
      plugins: { Dark },
      lang: quasarLang,
      config: {
        brand: {
          primary: '#6f2e2a',
          secondary: '#b08a3e',
          accent: '#b08a3e',
          dark: '#15140f',
          'dark-page': '#15140f',
          positive: '#2e7d32',
          negative: '#c62828',
          info: '#1565c0',
          warning: '#f9a825'
        }
      }
    }

    if (isClient) {
      app.use(Quasar, quasarOptions)
    } else {
      // Quasar's SSR install requires an ssrContext with a request object so
      // Platform.parseSSR can read the user-agent.
      const ssrContext: Record<string, unknown> = {
        req: { headers: { 'user-agent': 'vite-ssg' } }
      }
      ;(app.use as any)(Quasar, quasarOptions, ssrContext)
    }
    setupRouterGuards(router)

    if (isClient) {
      // Sync Quasar Dark plugin with the theme store
      const themeStore = useThemeStore()
      const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      const isDark = themeStore.theme === 'dark' || (themeStore.theme === 'system' && systemDark)
      Dark.set(isDark)
    }
  }
)