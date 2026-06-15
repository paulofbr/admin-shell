import { defineStore } from 'pinia'
import eventBus from '../utils/eventBus'

export interface ApplicationConfig {
  name?: string
  subtitle?: string
  icon?: string
  favicon?: string
  theme?: 'light' | 'dark'
}

const DEFAULT_CONFIG: ApplicationConfig = {
  name: 'Admin Shell',
  subtitle: 'Management Panel',
  icon: '',
  favicon: '',
  theme: 'light',
}

function isRemoteUrl(value: string | undefined): boolean {
  return !!value && (value.startsWith('http://') || value.startsWith('https://') || value.startsWith('data:') || value.startsWith('/'))
}

function updateFavicon(url: string | undefined): void {
  if (!url) return

  let link = document.querySelector<HTMLLinkElement>('link[rel="icon"]')
  if (!link) {
    link = document.createElement('link')
    link.rel = 'icon'
    document.head.appendChild(link)
  }

  link.href = url
}

export const useApplicationStore = defineStore('application', {
  state: (): { config: ApplicationConfig; initialized: boolean; unsubscribers: Array<() => void> } => ({
    config: { ...DEFAULT_CONFIG },
    initialized: false,
    unsubscribers: [],
  }),

  getters: {
    applicationName: state => state.config.name ?? DEFAULT_CONFIG.name,
    applicationSubtitle: state => state.config.subtitle ?? DEFAULT_CONFIG.subtitle,
    applicationIcon: state => state.config.icon ?? DEFAULT_CONFIG.icon,
    applicationFavicon: state => state.config.favicon ?? state.config.icon ?? DEFAULT_CONFIG.favicon,
  },

  actions: {
    init(): void {
      if (this.initialized) return
      this.initialized = true

      this.unsubscribers.push(
        eventBus.subscribe<ApplicationConfig>('application:configure', (config: ApplicationConfig) => {
          this.applyConfig(config)
        }),
        eventBus.subscribe('application:reset', () => {
          this.reset()
        }),
      )

      this.persistDom()
    },

    applyConfig(config: Partial<ApplicationConfig>): void {
      this.config = {
        ...this.config,
        ...config,
      }
      this.persistDom()
      eventBus.publish('application:configured', { ...this.config })
    },

    reset(): void {
      this.config = { ...DEFAULT_CONFIG }
      this.persistDom()
      eventBus.publish('application:configured', { ...this.config })
    },

    persistDom(): void {
      const name = this.applicationName ?? DEFAULT_CONFIG.name!
      const subtitle = this.applicationSubtitle ?? DEFAULT_CONFIG.subtitle!
      const favicon = this.applicationFavicon

      document.title = subtitle ? `${name} | ${subtitle}` : name
      document.documentElement.dataset.appName = name
      document.documentElement.dataset.appSubtitle = subtitle
      document.documentElement.dataset.appIcon = this.applicationIcon ?? ''

      if (isRemoteUrl(favicon)) {
        updateFavicon(favicon)
      }

      if (this.config.theme) {
        document.documentElement.dataset.theme = this.config.theme
      }
    },
  },
})
