import { defineStore } from 'pinia'
import eventBus from '@/utils/eventBus'
import pluginLoader from '@/components/plugins/PluginLoader'
import type {
  PluginDescriptor,
  PluginManifest,
  TableColumnContrib,
} from '@/types'
import * as pluginsApi from '@/api/plugins'

interface PluginState {
  plugins: PluginDescriptor[]
  manifests: Map<string, PluginManifest>
  tableColumns: TableColumnContrib[]
  isLoading: boolean
  discoveryPromise: Promise<void> | null
}

function isActivePluginStatus(status: number | string) {
  return status === 2 || status === 4 || status === 'loaded' || status === 'active'
}

export const usePluginStore = defineStore('plugin', {
  state: (): PluginState => ({
    plugins: [],
    manifests: new Map(),
    tableColumns: [],
    isLoading: false,
    discoveryPromise: null,
  }),

  actions: {
    initEventListeners() {
      // Listen for plugin column registrations
      eventBus.subscribe('plugin:table:register-column', (contrib: TableColumnContrib) => {
        // Replace if already registered by the same plugin + id
        const existing = this.tableColumns.findIndex(
          (c) => c.pluginId === contrib.pluginId && c.id === contrib.id
        )
        if (existing >= 0) {
          this.tableColumns[existing] = contrib
        } else {
          this.tableColumns.push(contrib)
        }
        // Force reactivity by replacing the array
        this.tableColumns = [...this.tableColumns]
      })
    },

    async loadPlugins() {
      this.isLoading = true
      try {
        const health = await pluginsApi.getHealth()
        this.plugins = health.plugins
      } catch (error) {
        console.error('Failed to load plugins:', error)
      } finally {
        this.isLoading = false
      }
    },

    isPluginActive(id: string) {
      return this.plugins.some(plugin => plugin.id === id && isActivePluginStatus(plugin.status))
    },

    async enablePlugin(id: string) {
      await pluginsApi.enablePlugin(id)
      await this.loadPluginManifests()
    },

    async disablePlugin(id: string) {
      await pluginsApi.disablePlugin(id)
      pluginLoader.unloadPlugin(id)
      this.manifests.delete(id)
      this.tableColumns = this.tableColumns.filter(column => column.pluginId !== id)
      this.rebuildContributionsFromManifests()
      await this.loadPluginManifests()
    },

    registerManifest(manifest: PluginManifest) {
      this.manifests.set(manifest.id, manifest)
      this.rebuildContributionsFromManifests()
    },

    async loadPluginManifests() {
      if (this.discoveryPromise) {
        return this.discoveryPromise
      }

      this.discoveryPromise = this.loadPluginManifestsCore()
        .finally(() => {
          this.discoveryPromise = null
        })

      return this.discoveryPromise
    },

    async loadPluginManifestsCore() {
      await this.loadPlugins()

      const activePluginIds = new Set(
        this.plugins
          .filter(plugin => isActivePluginStatus(plugin.status))
          .map(plugin => plugin.id)
      )
      const loadedPluginIds = new Set<string>()
      const manifests = await pluginLoader.scanPlugins()

      for (const manifest of manifests) {
        if (!activePluginIds.has(manifest.id)) continue
        if (loadedPluginIds.has(manifest.id)) continue

        try {
          this.registerManifest(manifest)

          // Load and initialize the frontend plugin module. Embedded manifests are
          // served from the backend; public manifests are still supported for demos.
          const instance = await pluginLoader.loadPlugin(manifest)
          if (instance) {
            console.log(`Frontend plugin loaded: ${instance.name} v${instance.version}`)
          }

          loadedPluginIds.add(manifest.id)
        } catch (error) {
          console.warn(`Could not load manifest for plugin ${manifest.id}:`, error)
        }
      }
    },

    rebuildContributionsFromManifests() {
      const activePluginIds = new Set(
        this.plugins
          .filter(plugin => isActivePluginStatus(plugin.status))
          .map(plugin => plugin.id)
      )

      for (const pluginId of Array.from(this.manifests.keys())) {
        if (!activePluginIds.has(pluginId)) {
          this.manifests.delete(pluginId)
        }
      }
    },
  },
})