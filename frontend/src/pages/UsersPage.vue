<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Users</h2>
        <p class="page__subtitle">Manage user accounts</p>
      </div>
      <div style="display: flex; gap: 8px; flex-wrap: wrap">
       
        <el-button :icon="Refresh" round @click="loadUsers" :loading="loading">
          Refresh
        </el-button>
        <el-button type="primary" :icon="Plus" round @click="openCreateDialog">
          Add User
        </el-button>
        <HeaderActions target="page.toolbar" target-page="users" />
      </div>
    </div>

    <!-- Filters from plugins -->
    <div v-if="pluginFilters.length > 0" class="users-filters">
      <div v-for="filter in pluginFilters" :key="filter.id" class="users-filters__item">
        <el-select
          v-model="filter.value"
          :placeholder="filter.label"
          clearable
          size="default"
          @change="loadUsers"
        >
          <el-option
            v-for="opt in filter.options"
            :key="opt.value"
            :label="opt.label"
            :value="opt.value"
          />
        </el-select>
      </div>
    </div>

    <el-card shadow="never">
      <ResponsiveGrid
        :data="users as unknown as GridRow[]"
        :columns="gridColumns"
        :filters="gridFilters"
        :loading="loading"
        :edit-mode="'popup'"
        empty-text="No users"
        @filter-change="handleFilterChange"
      >
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

        <template #cell-actions="{ row }">
          <el-button type="primary" size="small" text @click="openEditDialog(row as unknown as User)">Edit</el-button>
          <el-popconfirm
            title="Delete this user?"
            confirm-button-text="Delete"
            confirm-button-type="danger"
            @confirm="handleDelete(row as unknown as User)"
          >
            <template #reference>
              <el-button type="danger" size="small" text>Delete</el-button>
            </template>
          </el-popconfirm>
        </template>
      </ResponsiveGrid>
    </el-card>

    <!-- Create / Edit Dialog -->
    <el-dialog
      v-model="dialogVisible"
      width="90%"
      :style="{'--el-dialog-width': dialogWidth}"
      :close-on-click-modal="false"
      center
    >
      <EntityEditor
        :title="isEditing ? 'Edit User' : 'Add User'"
        :save-label="isEditing ? 'Save Changes' : 'Create User'"
        :save-loading="saving"
        :on-save="handleSave"
        :on-cancel="() => dialogVisible = false"
      >
        <el-form
          ref="formRef"
          :model="formModel"
          :rules="formRules"
          label-position="top"
          @submit.prevent="handleSave"
        >
        <el-form-item label="Email" prop="email">
          <el-input v-model="form.email" placeholder="user@example.com" />
        </el-form-item>
        <el-row :gutter="16" class="user-form-row">
              <el-col :span="12" class="user-form-col">
                <el-form-item label="Username" prop="username">
                  <el-input v-model="form.username" placeholder="johndoe" />
                </el-form-item>
              </el-col>
              <el-col :span="12" class="user-form-col">
                <el-form-item label="Display Name" prop="displayName">
                  <el-input v-model="form.displayName" placeholder="John Doe" />
                </el-form-item>
              </el-col>
            </el-row>
        <el-form-item v-if="!isEditing" label="Password" prop="password">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            placeholder="Enter password"
          />
        </el-form-item>
        <el-form-item v-if="isEditing" label="Active" prop="isActive">
          <el-switch v-model="form.isActive" />
        </el-form-item>

        <template v-for="field in activeExtraFields" :key="field.key">
          <el-form-item
            :label="field.label"
            :prop="field.key"
            :required="field.required"
          >
            <el-select
              v-if="field.inputType === 'select'"
              v-model="extraFields[field.key]"
              :placeholder="`Select ${field.label.toLowerCase()}`"
              clearable
            >
              <el-option
                v-for="option in field.options"
                :key="option.value"
                :label="option.label"
                :value="option.value"
              />
            </el-select>
            <el-input
              v-else-if="field.inputType === 'textarea'"
              v-model="extraFields[field.key]"
              type="textarea"
              :placeholder="field.placeholder"
              :rows="field.rows"
            />
            <el-input
              v-else-if="field.inputType === 'number'"
              v-model.number="extraFields[field.key]"
              type="number"
              :placeholder="field.placeholder"
            />
            <el-input
              v-else-if="field.inputType === 'date'"
              v-model="extraFields[field.key]"
              type="date"
              :placeholder="field.placeholder"
            />
            <el-input
              v-else
              v-model="extraFields[field.key]"
              :placeholder="field.placeholder"
            />
          </el-form-item>
        </template>
        </el-form>
      </EntityEditor>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { usePluginStore } from '@/stores/pluginStore'
import { useExtensionStore, type FormFieldDescriptor } from '@/stores/extensionStore'
import HeaderActions from '@/components/common/HeaderActions.vue'
import EntityEditor from '@/components/common/EntityEditor.vue'
import ResponsiveGrid, {
  type GridColumn,
  type GridFilter,
  type GridRow,
} from '@/components/common/ResponsiveGrid.vue'
import { useNotificationStore } from '@/stores/notificationStore'
import eventBus from '@/utils/eventBus'
import * as usersApi from '@/api/users'
import type { User } from '@/types'
import { ElMessage } from 'element-plus'
import { authApi } from '@/services/api'
import { Refresh, Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const pluginStore = usePluginStore()
const notificationStore = useNotificationStore()
const extensionStore = useExtensionStore()
const route = useRoute()

const users = ref<User[]>([])
const total = ref(0)
const skip = ref(0)
const take = 10
const loading = ref(false)
const deleting = ref(false)
const saving = ref(false)

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

// Debounce filter search
let filterDebounceTimer: ReturnType<typeof setTimeout> | null = null
function debouncedLoadUsers() {
  if (filterDebounceTimer) clearTimeout(filterDebounceTimer)
  filterDebounceTimer = setTimeout(() => {
    skip.value = 0 // Reset to first page
    loadUsers()
  }, 300)
}

function handleFilterChange(payload: { id: string; value: unknown }) {
  const value = payload.value === null || payload.value === undefined ? '' : String(payload.value)
  filters.value = {
    ...filters.value,
    [payload.id]: value,
  }
  debouncedLoadUsers()
}

// Responsive dialog width
const dialogWidth = computed(() => window.innerWidth < 768 ? '95%' : '520px')

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

// Dialog state
const dialogVisible = ref(false)
const isEditing = ref(false)
const editingUserId = ref<string | null>(null)
const formRef = ref<FormInstance>()

const form = ref({
  email: '',
  username: '',
  displayName: '',
  password: '',
  isActive: true,
})
const extraFields = ref<Record<string, any>>({})
const formModel = computed(() => ({ ...form.value, ...extraFields.value }))

const formRules: FormRules = {
  email: [
    { required: true, message: 'Email is required', trigger: 'blur' },
    { type: 'email', message: 'Invalid email format', trigger: 'blur' },
  ],
  username: [
    { required: true, message: 'Username is required', trigger: 'blur' },
    { min: 3, message: 'Username must be at least 3 characters', trigger: 'blur' },
  ],
  password: [
    {
      required: true,
      validator: (_: unknown, value: string, callback: Function) => {
        if (!isEditing.value && !value) {
          callback(new Error('Password is required'))
        } else {
          callback()
        }
      },
      trigger: 'blur',
    },
    { min: 6, message: 'Password must be at least 6 characters', trigger: 'blur' },
  ],
}

const activeExtraFields = computed<FormFieldDescriptor[]>(() =>
  extensionStore.getFormFieldsForForm(isEditing.value ? 'user.edit' : 'user.create'),
)

async function validateExtraFields() {
  for (const field of activeExtraFields.value) {
    const value = extraFields.value[field.key]
    if (field.required && (value === undefined || value === null || value === '')) {
      ElMessage.error(`${field.label} is required`)
      return false
    }

    if (field.validationPattern && value !== undefined && value !== null && value !== '') {
      const regex = new RegExp(field.validationPattern)
      if (!regex.test(String(value))) {
        ElMessage.error(field.validationMessage ?? `Invalid ${field.label.toLowerCase()}`)
        return false
      }
    }
  }

  return true
}

async function loadExtraFieldValues(userId: string) {
  extraFields.value = {}

  for (const field of activeExtraFields.value) {
    if (!field.loadValueEndpoint) continue

    const endpoint = field.loadValueEndpoint.replaceAll('{userId}', userId)
    const response = await authApi.get(endpoint)
    const payload = response.data
    const value = field.valuePath
      ? field.valuePath.split('.').reduce<any>((current, key) => current?.[key], payload)
      : payload

    if (value !== undefined) {
      extraFields.value[field.key] = value
    }
  }
}

async function submitExtraFields(userId: string) {
  for (const field of activeExtraFields.value) {
    if (!field.apiEndpoint) continue

    const payload = field.payloadPath
      ? { [field.payloadPath]: extraFields.value[field.key] }
      : extraFields.value[field.key]

    const endpoint = field.apiEndpoint.replaceAll('{userId}', userId)
    const method = field.submitMethod ?? 'PUT'
    if (method === 'PUT') {
      await authApi.put(endpoint, payload)
    } else if (method === 'POST') {
      await authApi.post(endpoint, payload)
    } else if (method === 'PATCH') {
      await authApi.patch(endpoint, payload)
    } else if (method === 'DELETE') {
      await authApi.delete(endpoint)
    } else {
      await authApi.get(endpoint)
    }
  }
}

function openCreateDialog() {
  isEditing.value = false
  editingUserId.value = null
  form.value = { email: '', username: '', displayName: '', password: '', isActive: true }
  extraFields.value = {}
  dialogVisible.value = true
}

function openEditDialog(user: User) {
  isEditing.value = true
  editingUserId.value = user.id
  form.value = {
    email: user.email ?? '',
    username: user.username ?? '',
    displayName: user.displayName ?? '',
    password: '',
    isActive: user.isActive ?? true,
  }
  extraFields.value = {}
  dialogVisible.value = true
  loadExtraFieldValues(user.id).catch(() => {})
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  const extraValid = await validateExtraFields()
  if (!extraValid) return

  saving.value = true
  try {
    let savedUser: User | undefined
    if (isEditing.value && editingUserId.value) {
      const updateData: usersApi.UpdateUserRequest = {
        email: form.value.email,
        username: form.value.username,
        displayName: form.value.displayName || undefined,
        isActive: form.value.isActive,
      }
      savedUser = await usersApi.updateUser(editingUserId.value, updateData)
      notificationStore.addNotification('User updated successfully', 'success')
    } else {
      savedUser = await usersApi.createUser({
        email: form.value.email,
        username: form.value.username,
        displayName: form.value.displayName || undefined,
        password: form.value.password,
      })
      notificationStore.addNotification('User created successfully', 'success')
    }
    if (savedUser?.id) {
      await submitExtraFields(savedUser.id)
    }
    dialogVisible.value = false
    loadUsers()
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to save user'
    notificationStore.addNotification(message, 'error')
  } finally {
    saving.value = false
  }
}

async function toggleActive(user: User) {
  try {
    await usersApi.updateUser(user.id, { isActive: !(user.isActive ?? true) })
    notificationStore.addNotification(
      `User ${(user.isActive ?? true) ? 'deactivated' : 'activated'} successfully`,
      'success'
    )
    loadUsers()
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to update user'
    notificationStore.addNotification(message, 'error')
  }
}

async function loadUsers() {
  loading.value = true
  try {
    const result = await usersApi.getUsers(skip.value, take, {
      email: filters.value.email || undefined,
      username: filters.value.username || undefined,
      displayName: filters.value.displayName || undefined,
    })
    users.value = result.data
    total.value = result.total

    // Notify plugins that users are loaded (they can fetch additional data)
    eventBus.publish('users:loaded', result.data)
  } catch (error: unknown) {
    const message =
      error instanceof Error ? error.message : 'Failed to load users'
    notificationStore.addNotification(message, 'error')
  } finally {
    loading.value = false
  }
}

watch(skip, loadUsers)
void loadUsers()
watch(
  () => route.fullPath,
  () => loadUsers(),
  { immediate: true },
)
onMounted(() => loadUsers())

async function handleDelete(row: User) {
  deleting.value = true
  try {
    await usersApi.deleteUser(row.id)
    notificationStore.addNotification('User deleted successfully', 'success')
    loadUsers()
  } catch (error: unknown) {
    const message =
      error instanceof Error ? error.message : 'Failed to delete user'
    notificationStore.addNotification(message, 'error')
  } finally {
    deleting.value = false
  }
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
  .page {
    max-width: 100%;
    overflow-x: hidden;
  }

  .page__header {
    flex-direction: column;
    gap: 12px;
    align-items: flex-start;
  }

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

.el-card :deep(.el-card__body) {
  overflow-x: hidden;
}
</style>