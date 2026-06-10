import { defineStore } from 'pinia'

export interface AppSetting {
  key: string
  value: string
  category: string
  description: string | null
  valueType: string
}

export interface SettingsState {
  settings: AppSetting[]
  categories: string[]
  loading: boolean
  saving: boolean
  error: string | null
}

function getAuthHeaders(): Record<string, string> {
  const token = localStorage.getItem('auth_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

const API_BASE = '/api'

export const useSettingsStore = defineStore('settings', {
  state: (): SettingsState => ({
    settings: [],
    categories: [],
    loading: false,
    saving: false,
    error: null,
  }),

  getters: {
    byCategory:
      (state) =>
      (category: string): AppSetting[] =>
        state.settings.filter((s) => s.category === category),

    getValue:
      (state) =>
      (key: string, fallback = ''): string => {
        const s = state.settings.find((s) => s.key === key)
        return s ? s.value : fallback
      },
  },

  actions: {
    async loadSettings() {
      this.loading = true
      this.error = null

      try {
        const res = await fetch(`${API_BASE}/settings`, {
          headers: getAuthHeaders(),
        })
        if (!res.ok) throw new Error(`Failed to load settings: ${res.status}`)
        this.settings = await res.json()

        const catRes = await fetch(`${API_BASE}/settings/categories`, {
          headers: getAuthHeaders(),
        })
        if (catRes.ok) {
          this.categories = await catRes.json()
        }
      } catch (e: unknown) {
        this.error = e instanceof Error ? e.message : 'Unknown error'
      } finally {
        this.loading = false
      }
    },

    async saveAllSettings(changes: Record<string, string>) {
      this.saving = true

      try {
        const payload = Object.entries(changes).map(([key, value]) => ({ key, value }))
        const res = await fetch(`${API_BASE}/settings`, {
          method: 'PUT',
          headers: { ...getAuthHeaders(), 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        })
        if (!res.ok) throw new Error(`Failed to save settings: ${res.status}`)
        for (const [key, value] of Object.entries(changes)) {
          const idx = this.settings.findIndex((s) => s.key === key)
          if (idx >= 0) this.settings[idx].value = value
        }
      } catch (e: unknown) {
        this.error = e instanceof Error ? e.message : 'Unknown error'
        throw e
      } finally {
        this.saving = false
      }
    },
  },
})