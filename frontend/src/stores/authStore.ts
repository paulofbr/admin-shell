import { defineStore } from 'pinia'
import type { User } from '@admin-shell/ui/types'
import * as authApi from '@/api/auth'

const TOKEN_KEY = 'adminshell-token'
const REFRESH_TOKEN_KEY = 'adminshell-refresh-token'
const LEGACY_TOKEN_KEY = 'auth_token'
const LEGACY_REFRESH_TOKEN_KEY = 'auth_refresh'

function setTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(TOKEN_KEY, accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  localStorage.setItem(LEGACY_TOKEN_KEY, accessToken)
  localStorage.setItem(LEGACY_REFRESH_TOKEN_KEY, refreshToken)
}

function clearTokens(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(LEGACY_TOKEN_KEY)
  localStorage.removeItem(LEGACY_REFRESH_TOKEN_KEY)
}


interface AuthState {
  user: User | null
  token: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    user: null,
    token: null,
    refreshToken: null,
    isAuthenticated: false,
    isLoading: true,
  }),
  actions: {
    async login(email: string, password: string) {
      const result = await authApi.login(email, password)
      setTokens(result.accessToken, result.refreshToken)

      let user: User | null = result.user ?? null
      if (!user) {
        try {
          user = await authApi.getMe()
        } catch {
          user = {
            id: '',
            email,
            username: email.split('@')[0],
            displayName: null,
            avatarUrl: null,
            isActive: true,
            createdAt: new Date().toISOString(),
            roles: [],
          }
        }
      }

      this.user = user
      this.token = result.accessToken
      this.refreshToken = result.refreshToken
      this.isAuthenticated = true
    },

    async register(email: string, username: string, password: string, displayName?: string) {
      const result = await authApi.register(email, username, password, displayName)
      setTokens(result.accessToken, result.refreshToken)

      let user: User | null = result.user ?? null
      if (!user) {
        try {
          user = await authApi.getMe()
        } catch {
          user = {
            id: '',
            email,
            username,
            displayName: displayName ?? null,
            avatarUrl: null,
            isActive: true,
            createdAt: new Date().toISOString(),
            roles: [],
          }
        }
      }

      this.user = user
      this.token = result.accessToken
      this.refreshToken = result.refreshToken
      this.isAuthenticated = true
    },

    async logout() {
      try {
        await authApi.logout()
      } catch {
        // ignore errors on logout
      }
      clearTokens()
      this.user = null
      this.token = null
      this.refreshToken = null
      this.isAuthenticated = false
    },

    setUser(user: User | null) {
      this.user = user
      this.isAuthenticated = !!user
    },

    async loadFromStorage() {
      const token = localStorage.getItem(TOKEN_KEY) ?? localStorage.getItem(LEGACY_TOKEN_KEY)
      const refreshToken =
        localStorage.getItem(REFRESH_TOKEN_KEY) ?? localStorage.getItem(LEGACY_REFRESH_TOKEN_KEY)

      if (!token || !refreshToken) {
        this.isLoading = false
        this.isAuthenticated = false
        return
      }

      this.token = token
      this.refreshToken = refreshToken

      try {
        const user = await authApi.getMe()
        this.user = user
        this.isAuthenticated = true
        this.isLoading = false
      } catch {
        clearTokens()
        this.user = null
        this.token = null
        this.refreshToken = null
        this.isAuthenticated = false
        this.isLoading = false
      }
    },
  },
})