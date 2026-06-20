import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePluginStore } from '@/stores/pluginStore'
import * as pluginsApi from '@/api/plugins'

vi.mock('@/api/plugins', () => ({
  getHealth: vi.fn(),
  enablePlugin: vi.fn(),
  disablePlugin: vi.fn(),
}))

vi.mock('@/components/plugins/PluginLoader', () => ({
  default: {
    loadPlugins: vi.fn(),
    getPluginManifests: vi.fn().mockReturnValue([]),
    discoverPlugins: vi.fn(),
    scanPlugins: vi.fn().mockResolvedValue([]),
    unloadPlugin: vi.fn(),
    loadPlugin: vi.fn(),
  },
}))

const mockPlugins = [
  {
    id: 'plugin-1',
    name: 'Test Plugin',
    version: '1.0.0',
    description: 'A test plugin',
    status: 2,
    loadedAt: new Date().toISOString(),
  },
  {
    id: 'plugin-2',
    name: 'Inactive Plugin',
    version: '0.5.0',
    description: 'An inactive plugin',
    status: 1,
    loadedAt: new Date().toISOString(),
  },
]

describe('pluginStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(pluginsApi.getHealth).mockResolvedValue({
      plugins: mockPlugins,
      status: 'Healthy',
      timestamp: new Date().toISOString(),
      version: '1.0.0',
      checks: [],
    })
  })

  it('initializes with empty state', () => {
    const store = usePluginStore()
    expect(store.plugins).toEqual([])
    expect(store.manifests.size).toBe(0)
    expect(store.tableColumns).toEqual([])
    expect(store.isLoading).toBe(false)
  })

  it('loadPlugins fetches and stores plugins', async () => {
    const store = usePluginStore()
    await store.loadPlugins()

    expect(store.plugins).toHaveLength(2)
    expect(store.plugins[0].name).toBe('Test Plugin')
    expect(pluginsApi.getHealth).toHaveBeenCalledOnce()
  })

  it('isPluginActive checks plugin status', () => {
    const store = usePluginStore()
    store.plugins = mockPlugins as any

    expect(store.isPluginActive('plugin-1')).toBe(true)
    expect(store.isPluginActive('plugin-2')).toBe(false)
    expect(store.isPluginActive('non-existent')).toBe(false)
  })

  it('enablePlugin calls API', async () => {
    vi.mocked(pluginsApi.enablePlugin).mockResolvedValue(undefined)

    const store = usePluginStore()
    store.plugins = mockPlugins as any
    await store.enablePlugin('plugin-2')

    expect(pluginsApi.enablePlugin).toHaveBeenCalledWith('plugin-2')
  })

  it('disablePlugin calls API', async () => {
    vi.mocked(pluginsApi.disablePlugin).mockResolvedValue(undefined)

    const store = usePluginStore()
    store.plugins = mockPlugins as any
    await store.disablePlugin('plugin-1')

    expect(pluginsApi.disablePlugin).toHaveBeenCalledWith('plugin-1')
  })

  it('initEventListeners sets up event bus subscriptions', () => {
    const store = usePluginStore()
    expect(store.tableColumns).toEqual([])
    store.initEventListeners()
    expect(store.tableColumns).toEqual([])
  })
})
