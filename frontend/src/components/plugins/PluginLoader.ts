import type {
  PluginManifest,
  FrontendPlugin,
  PluginServices,
} from '@/types'
import client from '@/api/client'
import eventBus from '@/utils/eventBus'

class PluginLoader {
  private loadedInstances: Map<string, FrontendPlugin> = new Map()

  async scanPlugins(): Promise<PluginManifest[]> {
    const manifests: PluginManifest[] = []
    try {
      const response = await fetch('/api/health')
      if (response.ok) {
        const health = await response.json()
        if (health.plugins && Array.isArray(health.plugins)) {
          for (const plugin of health.plugins) {
            try {
              const manifestResponse = await fetch(
                `/plugins/${plugin.id}/plugin.json`,
              )
              if (manifestResponse.ok) {
                const manifest: PluginManifest = await manifestResponse.json()
                manifests.push(manifest)
              }
            } catch {
              console.warn(`Could not load manifest for plugin ${plugin.id}`)
            }
          }
        }
      }
    } catch (error) {
      console.error('Failed to scan plugins:', error)
    }
    return manifests
  }

  async loadPlugin(manifest: PluginManifest): Promise<FrontendPlugin | null> {
    try {
      const url = `/plugins/${manifest.id}/${manifest.main}`

      // Files in /public/ are served as static assets and cannot be used with
      // dynamic import(). Instead, fetch the source, create a blob URL, and import it.
      const response = await fetch(url)
      if (!response.ok) {
        console.error(
          `Plugin ${manifest.id}: failed to fetch ${url} (HTTP ${response.status})`,
        )
        return null
      }

      const code = await response.text()
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

      const services: PluginServices = {
        http: client,
        eventBus: {
          publish: <T>(event: string, data: T) => eventBus.publish(event, data),
          subscribe: <T>(
            event: string,
            handler: (data: T) => void,
          ) => eventBus.subscribe(event, handler),
        },
        navigation: {
          navigate: (path: string) => {
            window.location.hash = path
          },
          getCurrentPath: () => window.location.hash.slice(1) || '/',
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
      }

      const container = document.createElement('div')
      container.id = `plugin-container-${manifest.id}`
      container.className = 'plugin-container'

      await pluginInstance.initialize(container, services)
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
}

export const pluginLoader = new PluginLoader()
export default pluginLoader