<template>
  <ListViewer title="Roles & Permissions" subtitle="Manage roles and their permissions">
    <ResponsiveGrid
      ref="gridRef"
      :columns="gridColumns"
      :edit-mode="'popup'"
      editable
      deletable
      :load-data="loadRoles"
      :editor-component="RoleEntityEditor"
      :get-editor-model="getRoleEditorModel"
      :delete-confirm-message="(row) => `Delete role ${(row as unknown as rolesApi.Role).name}?`"
      :on-delete="handleDelete"
      empty-text="No roles"
    >
      <template #toolbar>
        <el-button type="primary" round @click="openCreateDialog">
          Add Role
        </el-button>
      </template>

      <template #cell-role="{ row }">
        <div style="font-weight: 500">{{ (row as unknown as rolesApi.Role).name }}</div>
      </template>

      <template #cell-description="{ row }">
        <span v-if="(row as unknown as rolesApi.Role).description">
          {{ (row as unknown as rolesApi.Role).description }}
        </span>
        <span v-else style="color: var(--el-text-color-placeholder)">—</span>
      </template>

      <template #cell-permissions="{ row }">
        <el-button size="small" text type="primary" @click="managePermissions(row as unknown as rolesApi.Role)">
          Manage ({{ permissionCounts.get((row as unknown as rolesApi.Role).id) ?? 0 }})
        </el-button>
      </template>

      <template #cell-created="{ row }">
        <span style="font-size: 13px; color: var(--el-text-color-secondary)">
          {{ new Date((row as unknown as rolesApi.Role).createdAt).toLocaleDateString() }}
        </span>
      </template>
    </ResponsiveGrid>
  </ListViewer>

  <ResponsiveGrid
    ref="permissionsGridRef"
    :data="[]"
    :columns="[]"
    edit-mode="popup"
    :editor-component="RolePermissionsEditor"
    :get-editor-model="getPermissionsEditorModel"
    @editor-saved="loadRoles"
  />
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import ListViewer from '@admin-shell/ui/ListViewer.vue'
import ResponsiveGrid from '@admin-shell/ui/ResponsiveGrid.vue'
import RoleEntityEditor from '@/components/roles/RoleEntityEditor.vue'
import RolePermissionsEditor from '@/components/roles/RolePermissionsEditor.vue'
import type { GridColumn, GridDataLoader, GridLoadQuery, GridRow } from '@admin-shell/ui/types'
import * as rolesApi from '@/api/roles'

const gridRef = ref<InstanceType<typeof ResponsiveGrid>>()
const permissionsGridRef = ref<InstanceType<typeof ResponsiveGrid>>()

const gridColumns: GridColumn<rolesApi.Role>[] = [
  {
    id: 'role',
    label: 'Role',
    prop: 'name',
    minWidth: '200',
    filter: { type: 'text', placeholder: 'Filter role' },
  },
  {
    id: 'description',
    label: 'Description',
    prop: 'description',
    minWidth: '250',
    filter: { type: 'text', placeholder: 'Filter description' },
  },
  { id: 'permissions', label: 'Permissions', width: '250' },
  { id: 'created', label: 'Created', width: '140' },
]

const permissionCounts = ref(new Map<string, number>())

type RoleFilters = Record<string, string | null | undefined>
type RoleForm = rolesApi.Role

async function getRoleEditorModel(key: unknown, _row: GridRow): Promise<RoleForm> {
  return rolesApi.getRoleById(String(key))
}

async function getPermissionsEditorModel(key: unknown, _row: GridRow): Promise<string[]> {
  const response = await rolesApi.getRolePermissions(String(key))
  return response.assignedPermissionIds
}

const loadRoles: GridDataLoader<rolesApi.Role, RoleFilters> = async (query) => {
  const roles = await rolesApi.getRoles()
  const roleFilter = typeof query.filters.role === 'string' ? query.filters.role.trim().toLowerCase() : ''
  const descriptionFilter = typeof query.filters.description === 'string'
    ? query.filters.description.trim().toLowerCase()
    : ''

  const filteredRoles = roles.filter((role) => {
    const roleMatches = !roleFilter || role.name.toLowerCase().includes(roleFilter)
    const descriptionMatches = !descriptionFilter
      || (role.description ?? '').toLowerCase().includes(descriptionFilter)

    return roleMatches && descriptionMatches
  })

  const counts = new Map<string, number>()
  for (const r of filteredRoles) {
    try {
      const resp = await rolesApi.getRolePermissions(r.id)
      counts.set(r.id, resp.assigned.length)
    } catch { counts.set(r.id, 0) }
  }
  permissionCounts.value = counts

  return {
    data: filteredRoles,
    total: filteredRoles.length,
  }
}

async function handleDelete(row: GridRow): Promise<void> {
  const role = row as rolesApi.Role
  if (role.name === 'Admin') return
  await rolesApi.deleteRole(role.id)
}

async function managePermissions(role: rolesApi.Role) {
  permissionsGridRef.value?.openEditor(role as unknown as GridRow)
}

</script>

<style scoped>
@media (max-width: 768px) {
  .el-dialog {
    --el-dialog-width: 95% !important;
  }
}
</style>