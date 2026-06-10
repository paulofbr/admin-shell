import type { Permission } from '@/types'
import client from './client'

export interface Role {
  id: string
  name: string
  description: string | null
  createdAt: string
}

export interface RolePermissionsResponse {
  roleId: string
  assigned: Permission[]
  available: Permission[]
}

export async function getRoles(): Promise<Role[]> {
  const response = await client.get<Role[]>('/api/roles')
  return response.data
}

export async function getRoleById(id: string): Promise<Role> {
  const response = await client.get<Role>(`/api/roles/${id}`)
  return response.data
}

export async function createRole(data: { name: string; description?: string }): Promise<Role> {
  const response = await client.post<Role>('/api/roles', data)
  return response.data
}

export async function updateRole(id: string, data: { name?: string; description?: string }): Promise<Role> {
  const response = await client.put<Role>(`/api/roles/${id}`, data)
  return response.data
}

export async function deleteRole(id: string): Promise<void> {
  await client.delete(`/api/roles/${id}`)
}

export async function getAllPermissions(): Promise<Permission[]> {
  const response = await client.get<Permission[]>('/api/roles/permissions')
  return response.data
}

export async function getRolePermissions(id: string): Promise<RolePermissionsResponse> {
  const response = await client.get<RolePermissionsResponse>(`/api/roles/${id}/permissions`)
  return response.data
}

export async function assignPermission(roleId: string, permissionId: string): Promise<void> {
  await client.post(`/api/roles/${roleId}/permissions`, { permissionId })
}

export async function removePermission(roleId: string, permissionId: string): Promise<void> {
  await client.delete(`/api/roles/${roleId}/permissions/${permissionId}`)
}