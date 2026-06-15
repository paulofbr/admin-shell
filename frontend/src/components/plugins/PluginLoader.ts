import type {
  PluginDescriptor,
  PluginManifest,
  FrontendPlugin,
  PluginPermissions,
  PluginServices,
} from '@/types'
import { authApi } from '@/services/api'
import eventBus from '@/utils/eventBus'
import { registerPluginComponent, resolvePluginComponent } from '@/utils/pluginComponentRegistry'
import { registerPluginServices, unregisterPluginServices } from '@/utils/pluginServices'

function isActivePlugin(status: number | string): boolean {
  return status === 2 || status === 4 || status === 'loaded' || status === 'active'
}

async function fetchText(url: string): Promise<string> {
  const response = await authApi.get(url, { responseType: 'text' })
  if (typeof response.data === 'string') return response.data
  return JSON.stringify(response.data)
}

function readPluginPermissions(pluginModule: Record<string, unknown>): PluginPermissions | null {
  const permissions = pluginModule.permissions ?? pluginModule.pluginPermissions
  if (!permissions || typeof permissions !== 'object' || Array.isArray(permissions)) return null

  return Object.fromEntries(
    Object.entries(permissions).flatMap(([key, value]) => {
      if (!Array.isArray(value)) return []
      const codes = value.filter((item): item is string => typeof item === 'string')
      return codes.length > 0 ? [[key, codes]] : []
    }),
  )
}

class PluginLoader {
  private loadedInstances: Map<string, FrontendPlugin> = new Map()
  private loadedStyleUrls: Map<string, string[]> = new Map()

  async scanPlugins(): Promise<PluginManifest[]> {
    const manifests: PluginManifest[] = []

    try {
      const response = await fetch('/api/health')
      if (!response.ok) return manifests

      const health = await response.json()
      const plugins = Array.isArray(health.plugins) ? health.plugins : []

      for (const plugin of plugins as PluginDescriptor[]) {
        if (!isActivePlugin(plugin.status)) continue

        const embeddedManifest = await this.loadEmbeddedManifest(plugin)
        if (embeddedManifest) {
          manifests.push(embeddedManifest)
          continue
        }

        const externalManifest = await this.loadExternalManifest(plugin.id)
        if (externalManifest) {
          manifests.push(externalManifest)
        }
      }
    } catch (error) {
      console.error('Failed to scan plugins:', error)
    }

    return manifests
  }

  async loadPlugin(manifest: PluginManifest): Promise<FrontendPlugin | null> {
    try {
      const source = manifest.source ?? 'external'
      const main = manifest.main ?? 'index.js'
      const url = source === 'embedded'
        ? this.getAssetUrl(manifest, main)
        : `/plugins/${manifest.id}/${main.replace(/^\//, '')}`

      await this.loadStyles(manifest)

      // Files in /public/ are served as static assets and cannot be used with
      // dynamic import(). Instead, fetch the source, create a blob URL, and import it.
      const code = await fetchText(url)
      const blob = new Blob([code], { type: 'text/javascript' })
      const blobUrl = URL.createObjectURL(blob)
      const pluginModule = await import(/* @vite-ignore */ blobUrl)
      URL.revokeObjectURL(blobUrl)

      const PluginClass: new () => FrontendPlugin =
        pluginModule.default ?? pluginModule.Plugin

      if (!PluginClass) {
        console.error(
          `Plugin ${manifest.id} does not export a default or Plugin class`,
        )
        return null
      }

      const pluginInstance = new PluginClass()
      const permissions = readPluginPermissions(pluginModule)
      if (permissions) {
        pluginInstance.permissions = permissions
      }

      const services: PluginServices = {
        http: authApi,
        api: {
          pluginId: manifest.id,
          getUrl: (path) => `/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`,
          get: (path, config) => authApi.get(`/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`, config),
          post: (path, data, config) => authApi.post(`/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`, data, config),
          put: (path, data, config) => authApi.put(`/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`, data, config),
          patch: (path, data, config) => authApi.patch(`/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`, data, config),
          delete: (path, config) => authApi.delete(`/api/plugins/${manifest.id}/${path.replace(/^\//, '')}`, config),
        },
        eventBus: {
          publish: <T>(event: string, data: T) => eventBus.publish(event, data),
          subscribe: <T>(
            event: string,
            handler: (data: T) => void,
          ) => eventBus.subscribe(event, handler),
        },
        navigation: {
          navigate: (path: string) => {
            if (window.__adminShellRouter && window.location.pathname !== path) {
              window.__adminShellRouter.push(path)
              return
            }

            if (window.location.pathname !== path) {
              window.history.pushState({}, '', path)
              window.dispatchEvent(new PopStateEvent('popstate'))
            }
          },
          getCurrentPath: () => window.location.pathname || '/',
        },
        storage: {
          get: <T>(key: string): T | null => {
            const item = localStorage.getItem(`plugin:${manifest.id}:${key}`)
            return item ? (JSON.parse(item) as T) : null
          },
          set: <T>(key: string, value: T): void => {
            localStorage.setItem(
              `plugin:${manifest.id}:${key}`,
              JSON.stringify(value),
            )
          },
          remove: (key: string): void => {
            localStorage.removeItem(`plugin:${manifest.id}:${key}`)
          },
        },
        ui: {
          showNotification: (message, type) => {
            eventBus.publish('ui:notification', { message, type })
          },
          showModal: (component) => {
            eventBus.publish('ui:modal', { component })
          },
          showLoading: (show) => {
            eventBus.publish('ui:loading', { show })
          },
          registerTableColumn: (tableId, column) => {
            eventBus.publish('plugin:table:register-column', { tableId, ...column })
          },
        },
        components: {
          register: (name, component) => registerPluginComponent(name, component as any, manifest.id),
          resolve: (name) => resolvePluginComponent(name),
        },
        auth: {
          getToken: () => localStorage.getItem('auth_token'),
          isAuthenticated: () => !!localStorage.getItem('auth_token'),
          getUser: () => {
            const userStr = localStorage.getItem('auth_user')
            return userStr ? JSON.parse(userStr) : null
          },
        },
        theme: {
          getCurrentTheme: () =>
            (document.documentElement.getAttribute('data-theme') as
              | 'light'
              | 'dark') ?? 'light',
          toggleTheme: () => {
            const current =
              document.documentElement.getAttribute('data-theme') ?? 'light'
            const next = current === 'light' ? 'dark' : 'light'
            document.documentElement.setAttribute('data-theme', next)
          },
        },
        app: {
          configureApplication: (config) => {
            eventBus.publish('application:configure', config)
          },
          resetApplication: () => {
            eventBus.publish('application:reset', null)
          },
        },
      }

      const container = document.createElement('div')
      container.id = `plugin-container-${manifest.id}`
      container.className = 'plugin-container'

      await pluginInstance.initialize(container, services)
      registerPluginServices(manifest.id, services)
      this.loadedInstances.set(manifest.id, pluginInstance)

      return pluginInstance
    } catch (error) {
      console.error(`Failed to load plugin ${manifest.id}:`, error)
      return null
    }
  }

  unloadPlugin(id: string): void {
    const instance = this.loadedInstances.get(id)
    if (instance) {
      try {
        instance.dispose()
      } catch (error) {
        console.error(`Error disposing plugin ${id}:`, error)
      }
      this.loadedInstances.delete(id)
    }
    unregisterPluginServices(id)
    this.unloadStyles(id)
  }

  unloadAll(): void {
    this.loadedInstances.forEach((_instance, id) => {
      this.unloadPlugin(id)
    })
  }

  isLoaded(id: string): boolean {
    return this.loadedInstances.has(id)
  }

  getLoadedPlugins(): Map<string, FrontendPlugin> {
    return new Map(this.loadedInstances)
  }

  private async loadEmbeddedManifest(plugin: PluginDescriptor): Promise<PluginManifest | null> {
    const baseUrl = plugin.frontendBaseUrl ?? `/api/plugins/${plugin.id}/frontend`
    try {
      const manifest = await fetchText(`${baseUrl}/manifest.json`)
      return {
        ...JSON.parse(manifest) as PluginManifest,
        source: 'embedded',
        frontendBaseUrl: baseUrl,
      }
    } catch (error) {
      const status = (error as { response?: { status?: number } }).response?.status
      if (status !== 404) {
        console.warn(`Plugin ${plugin.id}: failed to load embedded frontend manifest`, error)
      }
      return null
    }
  }

  private async loadExternalManifest(pluginId: string): Promise<PluginManifest | null> {
    try {
      const manifestResponse = await fetch(`/plugins/${pluginId}/plugin.json`)
      if (!manifestResponse.ok) return null

      const manifest = await manifestResponse.json() as PluginManifest
      return {
        ...manifest,
        source: 'external',
      }
    } catch (error) {
      console.warn(`Could not load manifest for plugin ${pluginId}`, error)
      return null
    }
  }

  private async loadStyles(manifest: PluginManifest): Promise<void> {
    this.unloadStyles(manifest.id)

    const styleUrls = (manifest.styles ?? []).map((style) => this.getAssetUrl(manifest, style))
    const loadedUrls: string[] = []

    for (const url of styleUrls) {
      try {
        const css = await fetchText(url)
        const style = document.createElement('style')
        style.dataset.pluginStyle = manifest.id
        style.dataset.pluginStyleUrl = url
        style.textContent = `/* plugin:${manifest.id} */\n${css}`
        document.head.appendChild(style)
        loadedUrls.push(url)
      } catch (error) {
        console.warn(`Plugin ${manifest.id}: failed to apply style ${url}`, error)
      }
    }

    if (loadedUrls.length > 0) {
      this.loadedStyleUrls.set(manifest.id, loadedUrls)
    }
  }

  private unloadStyles(pluginId: string): void {
    Array.from(document.querySelectorAll<HTMLStyleElement>('style[data-plugin-style]'))
      .filter((style) => style.dataset.pluginStyle === pluginId)
      .forEach((style) => style.remove())
    this.loadedStyleUrls.delete(pluginId)
  }

  private getAssetUrl(manifest: PluginManifest, path: string): string {
    const normalized = path.replace(/^\//, '')
    if (manifest.source === 'embedded') {
      const baseUrl = manifest.frontendBaseUrl ?? `/api/plugins/${manifest.id}/frontend`
      return `${baseUrl}/${normalized}`
    }

    return `/plugins/${manifest.id}/${normalized}`
  }
}

export const pluginLoader = new PluginLoader()
export default pluginLoader
