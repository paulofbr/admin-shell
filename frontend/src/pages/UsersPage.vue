<template>
  <ListViewer title="Users" subtitle="Manage user accounts">
    <ResponsiveGrid
      ref="gridRef"
      :columns="gridColumns"
      :filters="gridFilters"
      :edit-mode="'popup'"
      editable
      deletable
      :load-data="loadUsers"
      :editor-component="UserEntityEditor"
      :get-editor-model="getUserEditorModel"
      :delete-confirm-message="(row) => `Delete user ${(row as unknown as User).email ?? (row as unknown as User).username ?? ''}?`"
      :on-delete="handleDelete"
      empty-text="No users"
      @filter-change="handleFilterChange"
    >
      <template #toolbar>
        <el-button :icon="Refresh" round @click="gridRef?.refresh()" :loading="gridRef?.isLoading">
          Refresh
        </el-button>
        <el-button type="primary" :icon="Plus" round @click="openCreateDialog">
          Add User
        </el-button>
        <HeaderActions target="page.toolbar" target-page="users" />
      </template>

      <template #cell-user="{ row }">
        <div style="display: flex; align-items: center; gap: 10px">
          <el-avatar :size="32" :src="(row as unknown as User).avatarUrl">
            {{ getUserInitial(row as unknown as User) }}
          </el-avatar>
          <div>
            <div style="font-weight: 500">{{ (row as unknown as User).displayName ?? (row as unknown as User).username ?? '—' }}</div>
            <div style="font-size: 12px; color: var(--el-text-color-secondary)">{{ (row as unknown as User).email ?? '—' }}</div>
          </div>
        </div>
      </template>

      <template #cell-username="{ value }">
        {{ value ?? '—' }}
      </template>

      <template
        v-for="col in pluginTableColumns"
        :key="`plugin-${col.id}`"
        #[`cell-${col.id}`]="{ row }"
      >
        <span v-html="col.render(row as any)" />
      </template>

      <template #cell-roles="{ row }">
        <el-tag
          v-for="role in ((row as unknown as User).roles ?? [])"
          :key="role.id"
          size="small"
          style="margin-right: 4px; margin-bottom: 2px"
        >
          {{ role.name }}
        </el-tag>
        <span v-if="((row as unknown as User).roles ?? []).length === 0" style="color: var(--el-text-color-placeholder)">—</span>
      </template>

      <template #cell-active="{ row }">
        <el-switch
          :model-value="(row as unknown as User).isActive"
          size="small"
          @click.prevent.stop
          @change="toggleActive(row as unknown as User)"
        />
      </template>

      <template #cell-created="{ row }">
        <span style="font-size: 13px; color: var(--el-text-color-secondary)">
          {{ new Date((row as unknown as User).createdAt).toLocaleDateString() }}
        </span>
      </template>
    </ResponsiveGrid>
  </ListViewer>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePluginStore } from '@/stores/pluginStore'
import HeaderActions from '@/components/common/HeaderActions.vue'
import UserEntityEditor from '@/components/users/UserEntityEditor.vue'
import ListViewer from '@admin-shell/ui/ListViewer.vue'
import ResponsiveGrid from '@admin-shell/ui/ResponsiveGrid.vue'
import type { GridColumn, GridFilter, GridLoadQuery, GridRow } from '@admin-shell/ui/types'
import { useNotificationStore } from '@/stores/notificationStore'
import eventBus from '@admin-shell/ui/event-bus'
import * as usersApi from '@/api/users'
import type { User } from '@admin-shell/ui/types'
import { Refresh, Plus } from '@element-plus/icons-vue'

const pluginStore = usePluginStore()
const notificationStore = useNotificationStore()

const gridRef = ref<InstanceType<typeof ResponsiveGrid>>()

// Inline column filters
const filters = ref({
  email: '',
  username: '',
  displayName: '',
})

function getUserInitial(user: User): string {
  const displayName = user.displayName?.trim()
  const username = user.username?.trim()
  return (displayName ?? username ?? '?')[0]?.toUpperCase() ?? '?'
}

const gridFilters: GridFilter[] = [
  { id: 'email', label: 'Email', type: 'text', placeholder: 'Filter email' },
  { id: 'username', label: 'Username', type: 'text', placeholder: 'Filter username' },
  { id: 'displayName', label: 'Display Name', type: 'text', placeholder: 'Filter name' },
]

function handleFilterChange(payload: { id: string; value: unknown }) {
  const value = payload.value === null || payload.value === undefined ? '' : String(payload.value)
  filters.value = {
    ...filters.value,
    [payload.id]: value,
  }
}

// Plugin contributions
const pluginTableColumns = computed(() => pluginStore.tableColumns)

const gridColumns = computed<GridColumn[]>(() => [
  { id: 'user', label: 'User', minWidth: '220' },
  { id: 'username', label: 'Username', prop: 'username', width: '130' },
  ...pluginTableColumns.value.map((col) => ({
    id: col.id,
    label: col.label,
    width: col.width,
  })),
  { id: 'roles', label: 'Roles', width: '180' },
  { id: 'active', label: 'Active', prop: 'isActive', width: '80' },
  { id: 'created', label: 'Created', width: '120' },
  { id: 'actions', label: 'Actions', width: '120', fixed: 'right' },
])

const pluginFilters = computed(() =>
  pluginStore.tableColumns
    .filter((col) => col.filterType && col.filterType !== 'none' && col.filterOptions)
    .map((col) => ({
      id: col.id,
      label: col.label,
      type: col.filterType as string,
      options: col.filterOptions ?? [],
      value: null as string | null,
    }))
)

async function toggleActive(user: User) {
  try {
    await usersApi.updateUser(user.id, { isActive: !(user.isActive ?? true) })
    notificationStore.addNotification(
      `User ${(user.isActive ?? true) ? 'deactivated' : 'activated'} successfully`,
      'success'
    )
    gridRef.value?.refresh()
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to update user'
    notificationStore.addNotification(message, 'error')
  }
}

async function loadUsers(query: GridLoadQuery): Promise<{ data: User[]; total: number }> {
  const result = await usersApi.getUsers(query.skip, query.pageSize, {
    email: stringFilter(query.filters.email),
    username: stringFilter(query.filters.username),
    displayName: stringFilter(query.filters.displayName),
  })

  // Notify plugins that users are loaded (they can fetch additional data)
  eventBus.publish('users:loaded', result.data)

  return {
    data: result.data,
    total: result.total,
  }
}

function stringFilter(value: unknown): string | undefined {
  const text = typeof value === 'string' ? value.trim() : ''
  return text || undefined
}

async function handleDelete(row: GridRow): Promise<void> {
  const user = row as User
  await usersApi.deleteUser(user.id)
}
</script>

<style scoped>
.users-filters {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.users-filters__item {
  min-width: 180px;
}

@media (max-width: 768px) {
  .users-filters {
    flex-direction: column;
    gap: 8px;
    width: 100%;
  }

  .users-filters__item {
    width: 100%;
    min-width: auto;
  }
}
</style>