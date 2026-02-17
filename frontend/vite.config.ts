import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Use environment variable for base path
  // Defaults to '/' for localhost development
  // Set VITE_BASE_PATH in GitHub Actions for deployment (e.g., '/repo-name/')
  base: process.env.VITE_BASE_PATH || '/',
})
