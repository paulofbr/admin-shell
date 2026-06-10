import { defineStore } from 'pinia'
import type { User } from '@/types'
import * as authApi from '@/api/auth'

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
      localStorage.setItem('auth_token', result.accessToken)
      localStorage.setItem('auth_refresh', result.refreshToken)

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
      localStorage.setItem('auth_token', result.accessToken)
      localStorage.setItem('auth_refresh', result.refreshToken)

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
      localStorage.removeItem('auth_token')
      localStorage.removeItem('auth_refresh')
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
      const token = localStorage.getItem('auth_token')
      const refreshToken = localStorage.getItem('auth_refresh')

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
        localStorage.removeItem('auth_token')
        localStorage.removeItem('auth_refresh')
        this.user = null
        this.token = null
        this.refreshToken = null
        this.isAuthenticated = false
        this.isLoading = false
      }
    },
  },
})