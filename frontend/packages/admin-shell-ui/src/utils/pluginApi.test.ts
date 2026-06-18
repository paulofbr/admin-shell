import { beforeEach, describe, expect, it, vi } from 'vitest'

const mocks = vi.hoisted(() => ({
  authApi: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}))

vi.mock('@admin-shell/ui/services/api', () => ({
  authApi: mocks.authApi,
}))

import { createPluginApi } from '@admin-shell/ui/plugin-api'

describe('createPluginApi', () => {
  beforeEach(() => {
    Object.values(mocks.authApi).forEach((fn) => fn.mockReset())
  })

  it('builds scoped plugin API URLs', () => {
    const api = createPluginApi('user-department')

    expect(api.pluginId).toBe('user-department')
    expect(api.getUrl('/departments')).toBe('/api/plugins/user-department/departments')
    expect(api.getUrl('departments')).toBe('/api/plugins/user-department/departments')
  })

  it('forwards GET calls to authApi with the scoped URL', async () => {
    const api = createPluginApi('useraudit')
    const config = { params: { userId: 'u1' } }

    await api.get('/audit', config)

    expect(mocks.authApi.get).toHaveBeenCalledWith('/api/plugins/useraudit/audit', config)
  })

  it('forwards mutation calls to authApi with the scoped URL and payload', async () => {
    const api = createPluginApi('user-department')
    const payload = { departmentId: 'd1' }
    const config = { params: { userId: 'u1' } }

    await api.post('/departments', payload, config)
    await api.put('/users/u1/department', payload, config)
    await api.patch('/departments/d1', payload, config)
    await api.delete('/departments/d1', config)

    expect(mocks.authApi.post).toHaveBeenCalledWith('/api/plugins/user-department/departments', payload, config)
    expect(mocks.authApi.put).toHaveBeenCalledWith('/api/plugins/user-department/users/u1/department', payload, config)
    expect(mocks.authApi.patch).toHaveBeenCalledWith('/api/plugins/user-department/departments/d1', payload, config)
    expect(mocks.authApi.delete).toHaveBeenCalledWith('/api/plugins/user-department/departments/d1', config)
  })
})
