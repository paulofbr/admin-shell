<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import EntityEditor from '@admin-shell/ui/EntityEditor.vue'
import { useNotificationStore } from '@/stores/notificationStore'
import * as rolesApi from '@/api/roles'
import type { Role } from '@/api/roles'

type ResponsiveSize = {
  mobile?: string
  desktop?: string
  breakpoint?: number
}

interface Props {
  modelValue?: string[]
  row?: Role | null
  close?: () => void
}

const defaultSize: ResponsiveSize = {
  mobile: '95%',
  desktop: '720px',
}

const props = defineProps<Props>()
const emit = defineEmits<{
  saved: [value: Role]
}>()

const notificationStore = useNotificationStore()

const assignedPermIds = ref<string[]>([])
const allPermissions = ref<rolesApi.RolePermissionsResponse['available']>([])
const loading = ref(false)
const saving = ref(false)

const editorTitle = computed(() => `Permissions: ${props.row?.name ?? ''}`)

watch(
  () => props.modelValue,
  (value) => {
    assignedPermIds.value = value ?? []
  },
  { immediate: true },
)

watch(
  () => props.row,
  async (role) => {
    if (!role) return

    loading.value = true
    try {
      const response = await rolesApi.getRolePermissions(role.id)
      allPermissions.value = response.available
      assignedPermIds.value = response.assignedPermissionIds
    } catch {
      notificationStore.addNotification('Failed to load permissions', 'error')
    } finally {
      loading.value = false
    }
  },
  { immediate: true },
)

const permissionGroups = computed(() => {
  const groups = new Map<string, rolesApi.RolePermissionsResponse['available']>()
  for (const permission of allPermissions.value) {
    if (!groups.has(permission.resource)) groups.set(permission.resource, [])
    groups.get(permission.resource)!.push(permission)
  }
  return Array.from(groups.entries()).map(([resource, permissions]) => ({ resource, permissions }))
})

async function save() {
  if (!props.row) return

  saving.value = true
  try {
    const updatedRole = await rolesApi.updateRolePermissions(props.row.id, assignedPermIds.value)
    notificationStore.addNotification('Permissions updated', 'success')
    emit('saved', updatedRole)
    props.close?.()
  } catch {
    notificationStore.addNotification('Failed to save permissions', 'error')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <EntityEditor
    :model-value="assignedPermIds"
    :title="editorTitle"
    :default-size="defaultSize"
    :save-label="'Save Permissions'"
    :save-loading="saving"
    :on-save="save"
    :on-cancel="close"
  >
    <el-checkbox-group v-model="assignedPermIds" v-loading="loading">
      <div v-for="group in permissionGroups" :key="group.resource" style="margin-bottom: 16px">
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
</template>
