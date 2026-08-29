import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
import { fileURLToPath, URL } from 'node:url'

declare module 'vite' {
  interface UserConfig {
    ssgOptions?: {
      includedRoutes?: string[] | (() => string[] | Promise<string[]>)
      dirStyle?: 'flat' | 'nested' | 'legacy'
    }
  }
}

// Quasar's vite plugin hardcodes runMode 'web-client', which defines
// __QUASAR_SSR_SERVER__ as false. vite-ssg's server build bundles quasar
// (ssr.noExternal) and needs the SSR-server flag so Platform.js skips
// browser-only `window` access during prerendering.
const quasarSsrDefines = (): any => ({
  name: 'quasar-ssr-defines',
  config(_config: unknown, env: { isSsrBuild?: boolean }) {
    if (env.isSsrBuild) {
      return {
        define: {
          __QUASAR_SSR__: 'true',
          __QUASAR_SSR_SERVER__: 'true',
          __QUASAR_SSR_CLIENT__: 'false',
          __QUASAR_SSR_PWA__: 'false'
        }
      }
    }
  }
})

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue({
      template: { transformAssetUrls }
    }),
    quasar({
      sassVariables: false
    }),
    quasarSsrDefines()
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  ssgOptions: {
    includedRoutes: () => ['/', '/podcast'],
    dirStyle: 'nested'
  },
  ssr: {
    noExternal: ['quasar', /^quasar\//]
  },
  server: {
    host: true,
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5001',
        changeOrigin: true
      },
      '/podcast/rss': {
        target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5001',
        changeOrigin: true
      },
      '/sitemap.xml': {
        target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5001',
        changeOrigin: true
      },
      '/uploads': {
        target: process.env.VITE_API_PROXY_TARGET || 'http://localhost:5001',
        changeOrigin: true
      }
    }
  }
})
