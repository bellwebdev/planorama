import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Dev-only proxy to the local API (backend http profile). In production the
    // app on Cloudflare Pages calls the API origin directly with CORS + JWT.
    proxy: {
      '/api': 'http://localhost:5202',
    },
  },
})
