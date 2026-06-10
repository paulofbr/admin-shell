import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useExtensionStore } from '@/stores/extensionStore'

describe('extensionStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('should initialize with empty arrays', () => {
    const store = useExtensionStore()
    expect(store.widgets).toEqual([])
    expect(store.tabs).toEqual([])
    expect(store.formFields).toEqual([])
    expect(store.headerActions).toEqual([])
    expect(store.reports).toEqual([])
    expect(store.sidebarSections).toEqual([])
    expect(store.pageResources).toEqual([])
  })

  it('should have filtering getters available', () => {
    const store = useExtensionStore()
    // These are computed properties, accessed as values
    expect(store.getWidgetsByZone).toBeDefined()
    expect(store.getTabsForPage).toBeDefined()
    expect(store.getFormFieldsForForm).toBeDefined()
    expect(store.getHeaderActions).toBeDefined()
    expect(store.getReportsByCategory).toBeDefined()
    expect(store.getSidebarSectionsSorted).toBeDefined()
    expect(store.getPageResourcesForRoute).toBeDefined()
  })

  it('should have loadExtensions and refresh methods', () => {
    const store = useExtensionStore()
    expect(store.loadExtensions).toBeDefined()
    expect(store.refresh).toBeDefined()
  })
})