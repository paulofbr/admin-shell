import { createApp } from 'vue'
import * as Vue from 'vue'
import { createPinia } from 'pinia'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import { i18n } from './i18n'
import { useAuthStore } from '@/stores/authStore'
import { usePluginStore } from '@/stores/pluginStore'
import { useApplicationStore } from '@/stores/applicationStore'
import './styles/index.css'

declare global {
  interface Window {
    Vue: typeof import('vue')
    ElementPlus: typeof ElementPlus
    __adminShellRouter: ReturnType<typeof import('vue-router')['createRouter']>
  }
}

window.Vue = Vue
window.ElementPlus = ElementPlus
window.__adminShellRouter = router

function ensurePluginImportMap(): void {
  if (document.querySelector('script[type="importmap"][data-admin-shell-plugin="true"]')) {
    return
  }

  const vueModuleUrl = URL.createObjectURL(new Blob([`
    const Vue = window.Vue
    export const createApp = Vue.createApp
    export const ref = Vue.ref
    export const reactive = Vue.reactive
    export const computed = Vue.computed
    export const defineComponent = Vue.defineComponent
    export const inject = Vue.inject
    export const onMounted = Vue.onMounted
    export const onUnmounted = Vue.onUnmounted
    export const openBlock = Vue.openBlock
    export const createElementBlock = Vue.createElementBlock
    export const createElementVNode = Vue.createElementVNode
    export const createVNode = Vue.createVNode
    export const createTextVNode = Vue.createTextVNode
    export const createCommentVNode = Vue.createCommentVNode
    export const Fragment = Vue.Fragment
    export const renderList = Vue.renderList
    export const toDisplayString = Vue.toDisplayString
    export const unref = Vue.unref
    export const vModelSelect = Vue.vModelSelect
    export const vModelText = Vue.vModelText
    export const withCtx = Vue.withCtx
    export const withDirectives = Vue.withDirectives
    export const withModifiers = Vue.withModifiers
    export default Vue
  `], { type: 'text/javascript' }))

  const elementPlusModuleUrl = URL.createObjectURL(new Blob([`
    const ElementPlus = window.ElementPlus
    export default ElementPlus
    export const ElButton = ElementPlus.ElButton
    export const ElCard = ElementPlus.ElCard
    export const ElCol = ElementPlus.ElCol
    export const ElForm = ElementPlus.ElForm
    export const ElFormItem = ElementPlus.ElFormItem
    export const ElInput = ElementPlus.ElInput
    export const ElOption = ElementPlus.ElOption
    export const ElRadio = ElementPlus.ElRadio
    export const ElRadioGroup = ElementPlus.ElRadioGroup
    export const ElRow = ElementPlus.ElRow
    export const ElSelect = ElementPlus.ElSelect
    export const ElSkeleton = ElementPlus.ElSkeleton
    export const ElStatistic = ElementPlus.ElStatistic
    export const ElTable = ElementPlus.ElTable
    export const ElTableColumn = ElementPlus.ElTableColumn
    export const ElTag = ElementPlus.ElTag
  `], { type: 'text/javascript' }))

  const importMap = document.createElement('script')
  importMap.type = 'importmap'
  importMap.dataset.adminShellPlugin = 'true'
  importMap.textContent = JSON.stringify({
    imports: {
      vue: vueModuleUrl,
      'element-plus': elementPlusModuleUrl,
    },
  }, null, 2)

  document.head.appendChild(importMap)
}

ensurePluginImportMap()

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(ElementPlus)
app.use(i18n)

// Initialize application configuration store before plugin discovery.
const applicationStore = useApplicationStore()
applicationStore.init()

// Initialize plugin discovery and frontend modules after Pinia/router are ready.
const pluginStore = usePluginStore()
pluginStore.initEventListeners()
pluginStore.loadPluginManifests().catch((error) => {
  console.warn('Failed to initialize frontend plugins:', error)
})

// Initialize auth store
const authStore = useAuthStore()
authStore.loadFromStorage()

app.mount('#app')