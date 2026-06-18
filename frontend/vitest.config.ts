import { defineConfig } from 'vitest/config'
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
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.{test,spec}.{ts,tsx}', 'packages/**/*.{test,spec}.{ts,tsx,vue}'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'src/main.ts',
        'src/router/index.ts',
        'src/stores/index.ts',
      ],
    },
  },
})