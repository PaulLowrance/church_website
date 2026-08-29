import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { quasar, transformAssetUrls } from '@quasar/vite-plugin'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue({
      template: { transformAssetUrls }
    }),
    quasar({
      sassVariables: false
    })
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
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
