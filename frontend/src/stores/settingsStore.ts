import { defineStore } from 'pinia'
import * as settingsApi from '@/api/settings'

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
        this.settings = (await settingsApi.getSettings()).map((setting) => ({
          key: setting.key,
          value: setting.value,
          category: setting.category,
          description: setting.description,
          valueType: setting.valueType ?? '',
        }))

        const categories = await settingsApi.getSettingCategories()
        this.categories = categories
      } catch (e: unknown) {
        this.error = e instanceof Error ? e.message : 'Unknown error'
      } finally {
        this.loading = false
      }
    },

    async saveAllSettings(changes: Record<string, string>) {
      this.saving = true

      try {
        const changesList = Object.entries(changes).map(([key, value]) => ({ key, value }))
        await settingsApi.saveSettings(Object.fromEntries(changesList.map(({ key, value }) => [key, value] as const)))
        for (const change of changesList) {
          const key = change.key
          const value = change.value
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