import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@admin-shell/ui/EntityEditor.vue': fileURLToPath(new URL('./packages/admin-shell-ui/src/components/EntityEditor.vue', import.meta.url)),
      '@admin-shell/ui/ResponsiveGrid.vue': fileURLToPath(new URL('./packages/admin-shell-ui/src/components/ResponsiveGrid.vue', import.meta.url)),
      '@admin-shell/ui/ListViewer.vue': fileURLToPath(new URL('./packages/admin-shell-ui/src/components/ListViewer.vue', import.meta.url)),
      '@admin-shell/ui/AiAssistantButton.vue': fileURLToPath(new URL('./packages/admin-shell-ui/src/components/AiAssistantButton.vue', import.meta.url)),
      '@admin-shell/ui/services/api': fileURLToPath(new URL('./packages/admin-shell-ui/src/services/api.ts', import.meta.url)),
      '@admin-shell/ui/plugin-api': fileURLToPath(new URL('./packages/admin-shell-ui/src/utils/pluginApi.ts', import.meta.url)),
      '@admin-shell/ui/plugin-component-registry': fileURLToPath(new URL('./packages/admin-shell-ui/src/utils/pluginComponentRegistry.ts', import.meta.url)),
      '@admin-shell/ui/plugin-services': fileURLToPath(new URL('./packages/admin-shell-ui/src/utils/pluginServices.ts', import.meta.url)),
      '@admin-shell/ui/event-bus': fileURLToPath(new URL('./packages/admin-shell-ui/src/utils/eventBus.ts', import.meta.url)),
      '@admin-shell/ui/http-client': fileURLToPath(new URL('./packages/admin-shell-ui/src/http-client/index.ts', import.meta.url)),
      '@admin-shell/ui/types': fileURLToPath(new URL('./packages/admin-shell-ui/src/types/index.ts', import.meta.url)),
      '@admin-shell/ui': fileURLToPath(new URL('./packages/admin-shell-ui/src/index.ts', import.meta.url)),
      '@admin-shell/ui/*': fileURLToPath(new URL('./packages/admin-shell-ui/src/*', import.meta.url)),
      vue: 'vue/dist/vue.esm-bundler.js',
    },
  },
  build: {
    chunkSizeWarningLimit: 1200,
    rollupOptions: {
      output: {
        manualChunks(id: string) {
          if (id.includes('node_modules')) {
            return 'vendor'
          }
          return undefined
        },
      },
      onwarn(warning, warn) {
        if (
          warning.code === 'INVALID_ANNOTATION' ||
          warning.message.includes('contains an annotation that Rollup cannot interpret') ||
          warning.message.includes('chunks are larger than 500 kB')
        ) {
          return
        }
        warn(warning)
      },
    },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: false,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5000',
        changeOrigin: true,
      },
    },
  },
})