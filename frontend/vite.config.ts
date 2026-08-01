import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'node:path'

function apiProxy(target: string) {
  return {
    target,
    changeOrigin: true,
    secure: false,
    // SPA navigations (Accept: text/html) must NOT be proxied — only XHR/fetch.
    bypass(req: { headers: { accept?: string } }) {
      if (req.headers.accept?.includes('text/html')) {
        return '/index.html'
      }
    },
  }
}

// Default matches API `http` launch profile (localhost:5154). Override with VITE_DEV_PROXY_TARGET if using https.
const proxyTarget = process.env.VITE_DEV_PROXY_TARGET ?? 'http://localhost:5154'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
  server: {
    port: 5173,
    proxy: {
      '/auth': apiProxy(proxyTarget),
      '/accounts': apiProxy(proxyTarget),
      '/account-requests': apiProxy(proxyTarget),
      '/card-requests': apiProxy(proxyTarget),
      '/cards': apiProxy(proxyTarget),
      '/transactions': apiProxy(proxyTarget),
      '/admin': apiProxy(proxyTarget),
      '/topup-requests': apiProxy(proxyTarget),
      '/budget-categories': apiProxy(proxyTarget),
      '/health': apiProxy(proxyTarget),
    },
  },
})
