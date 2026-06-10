import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import './styles/index.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(ElementPlus)

// Initialize auth store
const { useAuthStore } = await import('@/stores/authStore')
const authStore = useAuthStore()
authStore.loadFromStorage()

app.mount('#app')