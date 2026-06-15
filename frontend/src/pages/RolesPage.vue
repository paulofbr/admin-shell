<template>
  <ListViewer title="Roles & Permissions" subtitle="Manage roles and their permissions">
    <template #actions>
      <el-button type="primary" round @click="openCreateDialog">
        Add Role
      </el-button>
    </template>
    
    <ResponsiveGrid
      :data="roles as unknown as GridRow[]"
      :columns="gridColumns as unknown as GridColumn[]"
      :loading="loading"
      :edit-mode="'popup'"
      empty-text="No roles"
    >
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

      <template #cell-actions="{ row }">
        <el-button type="primary" size="small" text @click="openEditDialog(row as unknown as rolesApi.Role)">Edit</el-button>
        <el-popconfirm
          title="Delete this role?"
          confirm-button-text="Delete"
          confirm-button-type="danger"
          @confirm="handleDelete(row as unknown as rolesApi.Role)"
        >
          <template #reference>
            <el-button :disabled="(row as unknown as rolesApi.Role).name === 'Admin'" type="danger" size="small" text>Delete</el-button>
          </template>
        </el-popconfirm>
      </template>
    </ResponsiveGrid>
  </ListViewer>

  <!-- Create/Edit Dialog -->
    <el-dialog v-model="dialogVisible" width="480" :close-on-click-modal="false">
      <EntityEditor
        :title="isEditing ? 'Edit Role' : 'Add Role'"
        :save-label="isEditing ? 'Save Changes' : 'Create Role'"
        :save-loading="saving"
        :on-save="handleSave"
        :on-cancel="() => dialogVisible = false"
      >
        <el-form ref="formRef" :model="form" :rules="formRules" label-position="top" @submit.prevent="handleSave">
          <el-form-item label="Name" prop="name">
            <el-input v-model="form.name" placeholder="e.g. Editor" />
          </el-form-item>
          <el-form-item label="Description" prop="description">
            <el-input v-model="form.description" placeholder="Optional description" type="textarea" :rows="2" />
          </el-form-item>
        </el-form>
      </EntityEditor>
    </el-dialog>

  <!-- Permissions Dialog -->
    <el-dialog v-model="permsVisible" width="600" :close-on-click-modal="false">
      <EntityEditor
        :title="`Permissions: ${selectedRole?.name ?? ''}`"
        save-label="Save Permissions"
        :save-loading="permsSaving"
        :on-save="savePermissions"
        :on-cancel="() => permsVisible = false"
      >
        <el-checkbox-group v-model="assignedPermIds" v-loading="permsLoading">
          <div v-for="group in permissionGroups" :key="group.resource" style="margin-bottom: 16px;">
            <div style="font-weight: 600; margin-bottom: 8px; color: var(--el-text-color-primary); text-transform: capitalize;">
              {{ group.resource }}
            </div>
            <div style="display: flex; flex-wrap: wrap; gap: 8px;">
              <el-checkbox
                v-for="perm in group.permissions"
                :key="perm.id"
                :label="perm.id"
                :value="perm.id"
              >
                <span :title="perm.code">{{ perm.action }}</span>
              </el-checkbox>
            </div>
          </div>
        </el-checkbox-group>
      </EntityEditor>
    </el-dialog>
  </template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useNotificationStore } from '@/stores/notificationStore'
import ListViewer from '@/components/common/ListViewer.vue'
import EntityEditor from '@/components/common/EntityEditor.vue'
import ResponsiveGrid, {
  type GridColumn,
  type GridRow,
} from '@/components/common/ResponsiveGrid.vue'
import * as rolesApi from '@/api/roles'
import type { FormInstance, FormRules } from 'element-plus'

const notificationStore = useNotificationStore()

const gridColumns: GridColumn<rolesApi.Role>[] = [
  { id: 'role', label: 'Role', prop: 'name', minWidth: '200' },
  { id: 'description', label: 'Description', prop: 'description', minWidth: '250' },
  { id: 'permissions', label: 'Permissions', width: '250' },
  { id: 'created', label: 'Created', width: '140' },
  { id: 'actions', label: 'Actions', width: '140', fixed: 'right' },
]

const roles = ref<rolesApi.Role[]>([])
const loading = ref(false)
const saving = ref(false)
const permissionCounts = ref(new Map<string, number>())

// Dialog state
const dialogVisible = ref(false)
const isEditing = ref(false)
const editingRoleId = ref<string | null>(null)
const formRef = ref<FormInstance>()

const form = ref({ name: '', description: '' })

const formRules: FormRules = {
  name: [{ required: true, message: 'Name is required', trigger: 'blur' }],
}

// Permissions dialog
const permsVisible = ref(false)
const permsLoading = ref(false)
const permsSaving = ref(false)
const selectedRole = ref<rolesApi.Role | null>(null)
const allPermissions = ref<rolesApi.RolePermissionsResponse['available']>([])
const assignedPermIds = ref<string[]>([])

const permissionGroups = computed(() => {
  const groups = new Map<string, typeof allPermissions.value>()
  for (const p of allPermissions.value) {
    if (!groups.has(p.resource)) groups.set(p.resource, [])
    groups.get(p.resource)!.push(p)
  }
  return Array.from(groups.entries()).map(([resource, permissions]) => ({ resource, permissions }))
})

async function loadRoles() {
  loading.value = true
  try {
    roles.value = await rolesApi.getRoles()
    const counts = new Map<string, number>()
    for (const r of roles.value) {
      try {
        const resp = await rolesApi.getRolePermissions(r.id)
        counts.set(r.id, resp.assigned.length)
      } catch { counts.set(r.id, 0) }
    }
    permissionCounts.value = counts
  } catch (err: unknown) {
    notificationStore.addNotification('Failed to load roles', 'error')
  } finally {
    loading.value = false
  }
}

function openCreateDialog() {
  isEditing.value = false
  editingRoleId.value = null
  form.value = { name: '', description: '' }
  dialogVisible.value = true
}

function openEditDialog(role: rolesApi.Role) {
  isEditing.value = true
  editingRoleId.value = role.id
  form.value = { name: role.name, description: role.description ?? '' }
  dialogVisible.value = true
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    if (isEditing.value && editingRoleId.value) {
      await rolesApi.updateRole(editingRoleId.value, form.value)
      notificationStore.addNotification('Role updated', 'success')
    } else {
      await rolesApi.createRole(form.value)
      notificationStore.addNotification('Role created', 'success')
    }
    dialogVisible.value = false
    loadRoles()
  } catch (err: unknown) {
    notificationStore.addNotification('Failed to save role', 'error')
  } finally {
    saving.value = false
  }
}

async function handleDelete(role: rolesApi.Role) {
  try {
    await rolesApi.deleteRole(role.id)
    notificationStore.addNotification('Role deleted', 'success')
    loadRoles()
  } catch {
    notificationStore.addNotification('Failed to delete role', 'error')
  }
}

async function managePermissions(role: rolesApi.Role) {
  selectedRole.value = role
  permsVisible.value = true
  permsLoading.value = true
  try {
    const [permsResp, allPerms] = await Promise.all([
      rolesApi.getRolePermissions(role.id),
      rolesApi.getAllPermissions(),
    ])
    assignedPermIds.value = permsResp.assigned.map((p) => p.id)
    allPermissions.value = allPerms
  } catch {
    notificationStore.addNotification('Failed to load permissions', 'error')
  } finally {
    permsLoading.value = false
  }
}

async function savePermissions() {
  if (!selectedRole.value) return
  permsSaving.value = true
  try {
    const current = await rolesApi.getRolePermissions(selectedRole.value.id)
    const currentIds = new Set(current.assigned.map((p) => p.id))
    const newIds = new Set(assignedPermIds.value)

    const toAdd = [...newIds].filter((id) => !currentIds.has(id))
    const toRemove = [...currentIds].filter((id) => !newIds.has(id))

    await Promise.all([
      ...toAdd.map((id) => rolesApi.assignPermission(selectedRole.value!.id, id)),
      ...toRemove.map((id) => rolesApi.removePermission(selectedRole.value!.id, id)),
    ])

    notificationStore.addNotification('Permissions updated', 'success')
    permsVisible.value = false
    loadRoles()
  } catch {
    notificationStore.addNotification('Failed to save permissions', 'error')
  } finally {
    permsSaving.value = false
  }
}

onMounted(loadRoles)
</script>

<style scoped>
@media (max-width: 768px) {
  .el-dialog {
    --el-dialog-width: 95% !important;
  }
}
</style>