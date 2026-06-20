// @vitest-environment happy-dom
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/authStore'
import * as authApi from '@/api/auth'

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  getMe: vi.fn(),
}))

const mockUser = {
  id: 'user-1',
  email: 'test@test.com',
  username: 'testuser',
  displayName: 'Test User',
  avatarUrl: null,
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
  roles: [{ id: 'role-1', name: 'User' }],
}

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('initializes with unauthenticated state', () => {
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)
    expect(store.user).toBeNull()
    expect(store.token).toBeNull()
    expect(store.isLoading).toBe(true)
  })

  it('login sets user and tokens', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      accessToken: 'access-123',
      refreshToken: 'refresh-123',
      user: mockUser,
    })

    const store = useAuthStore()
    await store.login('test@test.com', 'password')

    expect(store.isAuthenticated).toBe(true)
    expect(store.token).toBe('access-123')
    expect(store.refreshToken).toBe('refresh-123')
    expect(store.user).toEqual(mockUser)
    expect(localStorage.getItem('adminshell-token')).toBe('access-123')
  })

  it('login falls back to getMe when user not in response', async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      accessToken: 'access-123',
      refreshToken: 'refresh-123',
      user: null,
    })
    vi.mocked(authApi.getMe).mockResolvedValue(mockUser)

    const store = useAuthStore()
    await store.login('test@test.com', 'password')

    expect(store.user).toEqual(mockUser)
    expect(authApi.getMe).toHaveBeenCalledOnce()
  })

  it('logout clears user and tokens', async () => {
    vi.mocked(authApi.logout).mockResolvedValue(undefined)
    vi.mocked(authApi.login).mockResolvedValue({
      accessToken: 'access-123',
      refreshToken: 'refresh-123',
      user: mockUser,
    })

    const store = useAuthStore()
    await store.login('test@test.com', 'password')
    await store.logout()

    expect(store.isAuthenticated).toBe(false)
    expect(store.user).toBeNull()
    expect(store.token).toBeNull()
    expect(localStorage.getItem('adminshell-token')).toBeNull()
  })

  it('register creates user and sets tokens', async () => {
    vi.mocked(authApi.register).mockResolvedValue({
      accessToken: 'access-456',
      refreshToken: 'refresh-456',
      user: mockUser,
    })

    const store = useAuthStore()
    await store.register('test@test.com', 'testuser', 'password', 'Test User')

    expect(store.isAuthenticated).toBe(true)
    expect(store.token).toBe('access-456')
    expect(store.user).toEqual(mockUser)
  })

  it('loadFromStorage restores session from localStorage', async () => {
    localStorage.setItem('adminshell-token', 'stored-token')
    localStorage.setItem('adminshell-refresh-token', 'stored-refresh')
    vi.mocked(authApi.getMe).mockResolvedValue(mockUser)

    const store = useAuthStore()
    await store.loadFromStorage()

    expect(store.token).toBe('stored-token')
    expect(store.isAuthenticated).toBe(true)
    expect(store.user).toEqual(mockUser)
    expect(store.isLoading).toBe(false)
  })

  it('loadFromStorage clears invalid tokens', async () => {
    localStorage.setItem('adminshell-token', 'invalid-token')
    localStorage.setItem('adminshell-refresh-token', 'invalid-refresh')
    vi.mocked(authApi.getMe).mockRejectedValue(new Error('Unauthorized'))

    const store = useAuthStore()
    await store.loadFromStorage()

    expect(store.isAuthenticated).toBe(false)
    expect(store.token).toBeNull()
    expect(localStorage.getItem('adminshell-token')).toBeNull()
  })

  it('setUser updates user and authentication state', () => {
    const store = useAuthStore()
    store.setUser(mockUser)
    expect(store.user).toEqual(mockUser)
    expect(store.isAuthenticated).toBe(true)

    store.setUser(null)
    expect(store.user).toBeNull()
    expect(store.isAuthenticated).toBe(false)
  })
})
