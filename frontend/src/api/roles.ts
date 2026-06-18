import {
  getAdminShellHostV1,
  type CreateRoleRequest,
  type PermissionDto,
  type RoleDto,
  type UpdateRoleRequest,
} from '@/generated/api/adminshell'
import type { ExtensionField } from '@admin-shell/ui/types'

export type Permission = PermissionDto

export interface CreateRoleWithExtensionsRequest {
  name: string
  description?: string | null
  extensionFields?: ExtensionField[]
}

export interface UpdateRoleWithExtensionsRequest {
  name?: string | null
  description?: string | null
  extensionFields?: ExtensionField[]
}

export interface Role {
  id: string
  name: string
  description: string | null
  createdAt: string
  extensionFields?: ExtensionField[]
}

const api = getAdminShellHostV1()

function normalizeRole(role: RoleDto): Role {
  return {
    id: role.id ?? '',
    name: role.name ?? '',
    description: role.description ?? null,
    createdAt: role.createdAt ?? '',
    extensionFields: role.extensionFields,
  }
}

export interface RolePermissionsResponse {
  roleId: string
  assigned: Permission[]
  available: Permission[]
}

export async function getRoles(): Promise<Role[]> {
  const response = await api.getApiRoles()
  return response.data.map(normalizeRole)
}

export async function getRoleById(id: string): Promise<Role> {
  const response = await api.getApiRolesId(id)
  return normalizeRole(response.data)
}

export async function createRole(
  data: CreateRoleWithExtensionsRequest,
): Promise<Role> {
  const response = await api.postApiRoles({
    name: data.name,
    description: data.description ?? null,
    extensionFields: data.extensionFields,
  } as unknown as CreateRoleRequest)
  return normalizeRole(response.data)
}

export async function updateRole(
  id: string,
  data: UpdateRoleWithExtensionsRequest,
): Promise<Role> {
  const response = await api.putApiRolesId(id, {
    name: data.name ?? '',
    description: data.description ?? null,
    extensionFields: data.extensionFields,
  } as unknown as UpdateRoleRequest)
  return normalizeRole(response.data)
}

export async function deleteRole(id: string): Promise<void> {
  await api.deleteApiRolesId(id)
}

export async function getAllPermissions(): Promise<Permission[]> {
  const response = await api.getApiRolesPermissions()
  return response.data as Permission[]
}

export async function getRolePermissions(id: string): Promise<RolePermissionsResponse> {
  const response = await api.getApiRolesIdPermissions(id)
  return response.data as RolePermissionsResponse
}

export async function assignPermission(roleId: string, permissionId: string): Promise<void> {
  await api.postApiRolesIdPermissions(roleId, { permissionId })
}

export async function removePermission(roleId: string, permissionId: string): Promise<void> {
  await api.deleteApiRolesIdPermissionsPermissionId(roleId, permissionId)
}
