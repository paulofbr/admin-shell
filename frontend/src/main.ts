import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import './styles/index.css'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(ElementPlus)

// Load auth state before mounting
const authStore = JSON.parse(localStorage.getItem('auth') || '{}')
if (authStore?.token) {
  const pinia = app._context.provides.pinia
  // Auth will be loaded on router guard
}

router.isReady().then(() => {
  const authStore = JSON.parse(localStorage.getItem('auth') || '{}')
}).finally(() => {
  app.mount('#app')
})