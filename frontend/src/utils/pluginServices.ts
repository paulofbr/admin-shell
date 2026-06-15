import type { PluginServices } from '@/types'

const pluginServices = new Map<string, PluginServices>()

export function registerPluginServices(pluginId: string, services: PluginServices): void {
  pluginServices.set(pluginId, services)
}

export function unregisterPluginServices(pluginId: string): void {
  pluginServices.delete(pluginId)
}

export function getPluginServices(pluginId: string): PluginServices | undefined {
  return pluginServices.get(pluginId)
}
