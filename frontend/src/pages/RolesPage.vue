<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Roles &amp; Permissions</h2>
        <p class="page__subtitle">Manage roles and their permissions</p>
      </div>
      <el-button type="primary" :icon="Plus" round @click="openCreateDialog">
        Add Role
      </el-button>
    </div>

    <el-card shadow="never">
      <div class="table-wrapper">
        <el-table v-loading="loading" :data="roles" stripe style="width: 100%" class="desktop-table">
          <!-- Desktop Table View -->
          <el-table-column label="Role" min-width="200">
            <template #default="{ row }">
              <div style="font-weight: 500">{{ row.name }}</div>
            </template>
          </el-table-column>
          <el-table-column prop="description" label="Description" min-width="250">
            <template #default="{ row }">
              <span v-if="row.description">{{ row.description }}</span>
              <span v-else style="color: var(--el-text-color-placeholder)">—</span>
            </template>
          </el-table-column>
          <el-table-column label="Permissions" width="250">
            <template #default="{ row }">
              <el-button size="small" text type="primary" @click="managePermissions(row)">
                Manage ({{ permissionCounts.get(row.id) ?? 0 }})
              </el-button>
            </template>
          </el-table-column>
          <el-table-column label="Created" width="140">
            <template #default="{ row }">
              <span style="font-size: 13px; color: var(--el-text-color-secondary)">
                {{ new Date(row.createdAt).toLocaleDateString() }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="140" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" size="small" text @click="openEditDialog(row)">Edit</el-button>
              <el-popconfirm
                title="Delete this role?"
                confirm-button-text="Delete"
                confirm-button-type="danger"
                @confirm="handleDelete(row)"
              >
                <template #reference>
                  <el-button :disabled="row.name === 'Admin'" type="danger" size="small" text>Delete</el-button>
                </template>
              </el-popconfirm>
            </template>
          </el-table-column>
        </el-table>

        <!-- Mobile Card View -->
        <div v-if="roles.length > 0" class="mobile-cards">
          <div v-for="role in roles" :key="role.id" class="role-card">
            <div class="role-card__header">
              <div>
                <span class="role-card__name">{{ role.name }}</span>
                <span v-if="role.description" class="role-card__description">{{ role.description }}</span>
              </div>
            </div>
            <div class="role-card__content">
              <div class="role-card__field">
                <span class="role-card__label">Permissions</span>
                <span class="role-card__value">
                  <el-button size="small" text type="primary" @click="managePermissions(role)">
                    Manage ({{ permissionCounts.get(role.id) ?? 0 }})
                  </el-button>
                </span>
              </div>
              <div class="role-card__field">
                <span class="role-card__label">Created</span>
                <span class="role-card__value">{{ new Date(role.createdAt).toLocaleDateString() }}</span>
              </div>
            </div>
            <div class="role-card__actions">
              <el-button type="primary" size="small" @click="openEditDialog(role)">Edit</el-button>
              <el-popconfirm
                title="Delete this role?"
                confirm-button-text="Delete"
                confirm-button-type="danger"
                @confirm="handleDelete(role)"
              >
                <template #reference>
                  <el-button :disabled="role.name === 'Admin'" type="danger" size="small">Delete</el-button>
                </template>
              </el-popconfirm>
            </div>
          </div>
        </div>
      </div>
    </el-card>

    <!-- Create/Edit Dialog -->
    <el-dialog v-model="dialogVisible" :title="isEditing ? 'Edit Role' : 'Add Role'" width="480" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="formRules" label-position="top" @submit.prevent="handleSave">
        <el-form-item label="Name" prop="name">
          <el-input v-model="form.name" placeholder="e.g. Editor" />
        </el-form-item>
        <el-form-item label="Description" prop="description">
          <el-input v-model="form.description" placeholder="Optional description" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">
          {{ saving ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Role' }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Permissions Dialog -->
    <el-dialog v-model="permsVisible" :title="`Permissions: ${selectedRole?.name ?? ''}`" width="600" :close-on-click-modal="false">
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
      <template #footer>
        <el-button @click="permsVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="permsSaving" @click="savePermissions">Save Permissions</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useNotificationStore } from '@/stores/notificationStore'
import * as rolesApi from '@/api/roles'
import { Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const notificationStore = useNotificationStore()

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
.page { padding: 24px; }
.page__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
.page__title { margin: 0; font-size: 22px; font-weight: 600; color: var(--el-text-color-primary); }
.page__subtitle { margin: 4px 0 0; font-size: 14px; color: var(--el-text-color-secondary); }

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .page { padding: 16px; }

  .page__header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .el-dialog {
    --el-dialog-width: 95% !important;
  }

  .table-wrapper {
    overflow-x: hidden;
  }
}

@media (min-width: 769px) {
  .table-wrapper {
    overflow-x: auto;
  }
}
</style>