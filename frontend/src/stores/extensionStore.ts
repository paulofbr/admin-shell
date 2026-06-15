import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as extensionsApi from '@/api/extensions'
import type { MenuItem } from '@/types'

export interface WidgetDescriptor {
  id: string
  title: string
  zone: string
  order: number
  width: number
  height: number
  componentName?: string
  settings?: Record<string, any>
}

export interface TabDescriptor {
  id: string
  label: string
  icon?: string
  targetPage: string
  order: number
  componentPath: string
  requiredPermissions: string[]
  props?: Record<string, any>
}

export interface FormFieldDescriptor {
  key: string
  label: string
  targetForm: string
  inputType: string
  required: boolean
  defaultValue?: string
  placeholder?: string
  rows?: number
  description?: string
  validationPattern?: string
  validationMessage?: string
  options?: { value: string; label: string; group?: string }[]
  order: number
  apiEndpoint?: string
  loadValueEndpoint?: string
  valuePath?: string
  submitMethod?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  payloadPath?: string
}

export interface HeaderActionDescriptor {
  id: string
  label: string
  icon?: string
  target: string
  targetPage?: string
  actionType: string
  actionValue?: string
  order: number
  requiredPermissions: string[]
}

export interface ReportDescriptor {
  id: string
  name: string
  description: string
  icon?: string
  category: string
  supportedFormats: string[]
  reportEndpoint: string
  order: number
  requiredPermissions: string[]
}

export interface SidebarSectionDescriptor {
  id: string
  label: string
  icon?: string
  order: number
  collapsed: boolean
  requiredPermissions: string[]
  items: SidebarMenuItem[]
}

export interface SidebarMenuItem {
  id: string
  label: string
  icon?: string
  path: string
  order: number
  requiredPermissions: string[]
  children?: SidebarMenuItem[]
}

export interface PageResourceDescriptor {
  type: string
  src: string
  includePages: string[]
  position: string
}

/**
 * Transforms flat menu items with ParentId into a tree structure with children.
 */
function buildMenuTree(items: MenuItem[]): MenuItem[] {
  const itemMap = new Map<string, MenuItem>()
  const roots: MenuItem[] = []

  // Clone items and initialize children arrays
  for (const item of items) {
    itemMap.set(item.id, { ...item, children: item.children ?? [] })
  }

  // Build parent-child relationships
  const allItems = Array.from(itemMap.values())
  for (const item of allItems) {
    if (item.parentId) {
      const parent = itemMap.get(item.parentId)
      if (parent) {
        parent.children!.push(item)
      }
    } else {
      roots.push(item)
    }
  }

  return roots.sort((a, b) => a.order - b.order)
}

export const useExtensionStore = defineStore('extensions', () => {
  const widgets = ref<WidgetDescriptor[]>([])
  const tabs = ref<TabDescriptor[]>([])
  const formFields = ref<FormFieldDescriptor[]>([])
  const headerActions = ref<HeaderActionDescriptor[]>([])
  const reports = ref<ReportDescriptor[]>([])
  const sidebarSections = ref<SidebarSectionDescriptor[]>([])
  const menuItems = ref<MenuItem[]>([])
  const pageResources = ref<PageResourceDescriptor[]>([])
  const loading = ref(false)
  const loaded = ref(false)

  // Getters with filters
  const getWidgetsByZone = computed(() => (zone: string) =>
    widgets.value.filter(w => w.zone === zone).sort((a, b) => a.order - b.order)
  )

  const getTabsForPage = computed(() => (targetPage: string) =>
    tabs.value.filter(t => t.targetPage === targetPage).sort((a, b) => a.order - b.order)
  )

  const getFormFieldsForForm = computed(() => (targetForm: string) =>
    formFields.value.filter(f => f.targetForm === targetForm).sort((a, b) => a.order - b.order)
  )

  const getHeaderActions = computed(() => (target: string, targetPage?: string) =>
    headerActions.value
      .filter(a => {
        if (a.target !== target) return false
        if (target === 'page.toolbar' && a.targetPage !== targetPage) return false
        return true
      })
      .sort((a, b) => a.order - b.order)
  )

  const getReportsByCategory = computed(() => (category: string) =>
    reports.value.filter(r => r.category === category).sort((a, b) => a.order - b.order)
  )

  const getSidebarSectionsSorted = computed(() =>
    [...sidebarSections.value].sort((a, b) => a.order - b.order)
  )

  const menuItemsSorted = computed(() =>
    buildMenuTree([...menuItems.value])
  )

  const getPageResourcesForRoute = computed(() => (route: string) =>
    pageResources.value.filter(r => r.includePages.length === 0 || r.includePages.includes(route))
  )

  async function loadExtensions() {
    if (loaded.value) return
    loading.value = true
    try {
      const snapshot = await extensionsApi.getExtensions()
      if (snapshot) {
        widgets.value = snapshot.widgets as unknown as WidgetDescriptor[]
        tabs.value = snapshot.tabs as unknown as TabDescriptor[]
        formFields.value = snapshot.formFields as unknown as FormFieldDescriptor[]
        headerActions.value = snapshot.headerActions as unknown as HeaderActionDescriptor[]
        reports.value = snapshot.reports as unknown as ReportDescriptor[]
        sidebarSections.value = snapshot.sidebarSections as unknown as SidebarSectionDescriptor[]
        menuItems.value = snapshot.menuItems as unknown as MenuItem[]
        pageResources.value = snapshot.pageResources as unknown as PageResourceDescriptor[]
        loaded.value = true
      }
    } catch (e) {
      console.warn('Failed to load extensions:', e)
    } finally {
      loading.value = false
    }
  }

  async function refresh() {
    loaded.value = false
    await loadExtensions()
  }

  return {
    widgets, tabs, formFields, headerActions, reports, sidebarSections, menuItems, pageResources,
    loading, loaded,
    getWidgetsByZone, getTabsForPage, getFormFieldsForForm,
    getHeaderActions, getReportsByCategory, getSidebarSectionsSorted,
    menuItemsSorted, getPageResourcesForRoute,
    loadExtensions, refresh,
  }
})