import { authApi } from '@admin-shell/ui/services/api'

function joinPluginPath(pluginId: string, path: string) {
  const normalized = path.startsWith('/') ? path.slice(1) : path
  return `/api/plugins/${pluginId}/${normalized}`
}

export function createPluginApi(pluginId: string) {
  return {
    pluginId,
    getUrl(path: string) {
      return joinPluginPath(pluginId, path)
    },
    get(path: string, config?: any) {
      return authApi.get(joinPluginPath(pluginId, path), config)
    },
    post(path: string, data?: unknown, config?: any) {
      return authApi.post(joinPluginPath(pluginId, path), data, config)
    },
    put(path: string, data?: unknown, config?: any) {
      return authApi.put(joinPluginPath(pluginId, path), data, config)
    },
    patch(path: string, data?: unknown, config?: any) {
      return authApi.patch(joinPluginPath(pluginId, path), data, config)
    },
    delete(path: string, config?: any) {
      return authApi.delete(joinPluginPath(pluginId, path), config)
    },
  }
}

export const pluginApi = createPluginApi
