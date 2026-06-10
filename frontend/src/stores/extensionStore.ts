import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi } from '@/services/api'
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
  description?: string
  validationPattern?: string
  validationMessage?: string
  options?: { value: string; label: string; group?: string }[]
  order: number
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

interface ExtensionsSnapshot {
  widgets: WidgetDescriptor[]
  tabs: TabDescriptor[]
  formFields: FormFieldDescriptor[]
  headerActions: HeaderActionDescriptor[]
  reports: ReportDescriptor[]
  sidebarSections: SidebarSectionDescriptor[]
  menuItems: MenuItem[]
  pageResources: PageResourceDescriptor[]
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
      const res = await authApi.get('/api/extensions')
      if (res && res.data) {
        const snapshot = res.data as ExtensionsSnapshot
        widgets.value = snapshot.widgets || []
        tabs.value = snapshot.tabs || []
        formFields.value = snapshot.formFields || []
        headerActions.value = snapshot.headerActions || []
        reports.value = snapshot.reports || []
        sidebarSections.value = snapshot.sidebarSections || []
        menuItems.value = snapshot.menuItems || []
        pageResources.value = snapshot.pageResources || []
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