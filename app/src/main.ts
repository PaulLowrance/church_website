import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createHead } from '@unhead/vue/client'
import { Quasar, Dark } from 'quasar'
import quasarLang from 'quasar/lang/en-US'
import '@quasar/extras/material-icons/material-icons.css'
import 'quasar/src/css/index.sass'
import './style.css'
import App from './App.vue'
import router from './router'
import { useThemeStore } from '@/stores/theme'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(createHead())
app.use(Quasar, {
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
})

// Sync Quasar Dark plugin with the theme store
const themeStore = useThemeStore()
const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
const isDark = themeStore.theme === 'dark' || (themeStore.theme === 'system' && systemDark)
Dark.set(isDark)

app.mount('#app')
