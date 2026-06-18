import { resolve } from 'node:path'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  logLevel: 'silent',
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  plugins: [vue()],
  resolve: {
    alias: {
      '@admin-shell/ui': resolve(__dirname, '../../../frontend/packages/admin-shell-ui/src'),
    },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: false,
    cssCodeSplit: true,
    lib: {
      entry: resolve(__dirname, 'src/OrderCreationPlugin.ts'),
      name: 'OrderCreationPlugin',
      formats: ['es'],
      fileName: () => 'index.js',
    },
    rollupOptions: {
      onwarn(warning, warn) {
        if (
          warning.code === 'ANNOTATION_ON_UNUSED_NAMESPACE'
          && String(warning.id ?? '').includes('@vueuse/core')
        ) {
          return
        }

        warn(warning)
      },
      output: {
        entryFileNames: 'index.js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith('.css')) {
            return 'styles.css'
          }

          return '[name][extname]'
        },
      },
    },
  },
})
