import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Dev-only proxy to the local API. Targets the Caddy proxy from `docker compose up`
    // (docker-compose.yml maps it to host 8080 — the api container itself has no host port).
    // Running the backend directly via `dotnet run` instead uses port 5202 (launchSettings.json);
    // swap the target below if you're using that workflow. In production the app on Cloudflare
    // Pages calls the API origin directly with CORS + JWT instead of this proxy.
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
})
