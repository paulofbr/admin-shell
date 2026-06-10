import { defineStore } from 'pinia'
import eventBus from '@/utils/eventBus'
import pluginLoader from '@/components/plugins/PluginLoader'
import type {
  PluginDescriptor,
  PluginManifest,
  MenuItem,
  WidgetDescriptor,
  TableColumnContrib,
} from '@/types'
import * as pluginsApi from '@/api/plugins'

interface PluginState {
  plugins: PluginDescriptor[]
  manifests: Map<string, PluginManifest>
  menuItems: MenuItem[]
  widgets: WidgetDescriptor[]
  tableColumns: TableColumnContrib[]
  isLoading: boolean
}

export const usePluginStore = defineStore('plugin', {
  state: (): PluginState => ({
    plugins: [],
    manifests: new Map(),
    menuItems: [],
    widgets: [],
    tableColumns: [],
    isLoading: false,
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

    async enablePlugin(id: string) {
      await pluginsApi.enablePlugin(id)
      const health = await pluginsApi.getHealth()
      this.plugins = health.plugins
    },

    async disablePlugin(id: string) {
      await pluginsApi.disablePlugin(id)
      const health = await pluginsApi.getHealth()
      this.plugins = health.plugins
    },

    registerManifest(manifest: PluginManifest) {
      this.manifests.set(manifest.id, manifest)

      const allMenuItems: MenuItem[] = []
      const allWidgets: WidgetDescriptor[] = []

      this.manifests.forEach((m) => {
        if (m.uiContributions?.menuItems) {
          allMenuItems.push(...m.uiContributions.menuItems)
        }
        if (m.uiContributions?.widgets) {
          allWidgets.push(...m.uiContributions.widgets)
        }
      })

      allMenuItems.sort((a, b) => a.order - b.order)
      allWidgets.sort((a, b) => a.order - b.order)

      this.menuItems = allMenuItems
      this.widgets = allWidgets
    },

    async loadPluginManifests() {
      await this.loadPlugins()

      for (const plugin of this.plugins) {
        try {
          const response = await fetch(`/plugins/${plugin.id}/plugin.json`)
          if (response.ok) {
            const manifest: PluginManifest = await response.json()
            this.registerManifest(manifest)

            // Load and initialize the frontend plugin module
            const instance = await pluginLoader.loadPlugin(manifest)
            if (instance) {
              console.log(`Frontend plugin loaded: ${instance.name} v${instance.version}`)
            }
          }
        } catch (error) {
          console.warn(`Could not load manifest for plugin ${plugin.id}:`, error)
        }
      }
    },
  },
})