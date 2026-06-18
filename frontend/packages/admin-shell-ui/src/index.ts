export { default as EntityEditor } from './components/EntityEditor.vue'
export { default as ExtensionFieldFormItem } from './components/ExtensionFieldFormItem.vue'
export { default as ExtensionFieldSlots } from './components/ExtensionFieldSlots.vue'
export { default as ResponsiveGrid } from './components/ResponsiveGrid.vue'
export { default as ListViewer } from './components/ListViewer.vue'
export { default as AiAssistantButton } from './components/AiAssistantButton.vue'

export type {
  GridColumn,
  GridDataLoader,
  GridEditMode,
  GridFilter,
  GridFilterOption,
  GridLoadQuery,
  GridLoadResult,
  GridMobileField,
  GridRow,
} from './components/ResponsiveGrid.vue'
export type { ResponsiveSize } from './components/EntityEditor.vue'

export { httpClient } from './http-client/index'
export { default as axiosClient } from './http-client/index'
export { useNotificationStore } from './stores/notificationStore'
export { authApi } from './services/api'
export { createPluginApi, pluginApi } from './utils/pluginApi'
export { eventBus } from './utils/eventBus'
export {
  getPluginComponentOwner,
  listPluginComponents,
  onPluginComponentRegistered,
  registerPluginComponent,
  resolvePluginComponent,
} from './utils/pluginComponentRegistry'
export {
  getPluginServices,
  registerPluginServices,
  unregisterPluginServices,
} from './utils/pluginServices'

export type * from './types/index'
