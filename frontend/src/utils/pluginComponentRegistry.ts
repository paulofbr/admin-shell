import type { Component } from 'vue'

type ComponentRegistrationHandler = (componentName: string) => void

const GLOBAL_COMPONENTS_KEY = '__adminShellPluginComponents'
const GLOBAL_OWNERS_KEY = '__adminShellPluginComponentOwners'
const GLOBAL_LISTENERS_KEY = '__adminShellPluginComponentListeners'

declare global {
  interface Window {
    [GLOBAL_COMPONENTS_KEY]?: Map<string, Component>
    [GLOBAL_OWNERS_KEY]?: Map<string, string>
    [GLOBAL_LISTENERS_KEY]?: Set<ComponentRegistrationHandler>
  }
}

function getComponents(): Map<string, Component> {
  if (!window[GLOBAL_COMPONENTS_KEY]) {
    window[GLOBAL_COMPONENTS_KEY] = new Map<string, Component>()
  }
  return window[GLOBAL_COMPONENTS_KEY]!
}

function getComponentOwners(): Map<string, string> {
  if (!window[GLOBAL_OWNERS_KEY]) {
    window[GLOBAL_OWNERS_KEY] = new Map<string, string>()
  }
  return window[GLOBAL_OWNERS_KEY]!
}

function getListeners(): Set<ComponentRegistrationHandler> {
  if (!window[GLOBAL_LISTENERS_KEY]) {
    window[GLOBAL_LISTENERS_KEY] = new Set<ComponentRegistrationHandler>()
  }
  return window[GLOBAL_LISTENERS_KEY]!
}

export function registerPluginComponent(name: string, component: Component, pluginId?: string) {
  const next = new Map(getComponents())
  next.set(name, component)
  window[GLOBAL_COMPONENTS_KEY] = next

  if (pluginId) {
    const owners = new Map(getComponentOwners())
    owners.set(name, pluginId)
    window[GLOBAL_OWNERS_KEY] = owners
  }

  getListeners().forEach((listener) => listener(name))
}

export function resolvePluginComponent(name: string) {
  return getComponents().get(name)
}

export function getPluginComponentOwner(name: string): string | undefined {
  return getComponentOwners().get(name)
}

export function listPluginComponents() {
  return Array.from(getComponents().keys())
}

export function onPluginComponentRegistered(handler: ComponentRegistrationHandler) {
  getListeners().add(handler)

  return () => {
    getListeners().delete(handler)
  }
}
