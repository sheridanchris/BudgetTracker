import { defineConfig } from 'vite'

export default defineConfig({
  root: "src/BudgetTracker.Client",
  server: {
    proxy: {
      '/api': {
        target: 'http://0.0.0.0:5000',
        changeOrigin: true,
      }
    }
  }
})
