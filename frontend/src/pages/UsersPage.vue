<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Users</h2>
        <p class="page__subtitle">Manage user accounts</p>
      </div>
      <div style="display: flex; gap: 8px">
        <el-button :icon="Refresh" round @click="loadUsers" :loading="loading">
          Refresh
        </el-button>
        <el-button type="primary" :icon="Plus" round @click="openCreateDialog">
          Add User
        </el-button>
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
      <!-- Desktop Table View -->
      <div class="table-wrapper">
        <el-table
          v-loading="loading"
          :data="users"
          stripe
          style="width: 100%"
          @sort-change="handleSort"
          class="desktop-table"
        >
          <!-- Filter row -->
          <el-table-column label="" width="1">
            <template #header>
              <el-input
                v-model="filters.email"
                placeholder="Filter email"
                size="small"
                clearable
                @input="debouncedLoadUsers"
              />
            </template>
          </el-table-column>
          
          <!-- Built-in columns -->
          <el-table-column label="User" min-width="220">
            <template #header>
              <el-input
                v-model="filters.displayName"
                placeholder="Filter name"
                size="small"
                clearable
                @input="debouncedLoadUsers"
              />
            </template>
            <template #default="{ row }">
              <div style="display: flex; align-items: center; gap: 10px">
                <el-avatar :size="32" :src="row.avatarUrl">
                  {{ row.displayName?.[0] ?? row.username[0] }}
                </el-avatar>
                <div>
                  <div style="font-weight: 500">{{ row.displayName ?? row.username }}</div>
                  <div style="font-size: 12px; color: var(--el-text-color-secondary)">{{ row.email }}</div>
                </div>
              </div>
            </template>
          </el-table-column>
          <el-table-column prop="username" label="Username" width="130">
            <template #header>
              <el-input
                v-model="filters.username"
                placeholder="Filter username"
                size="small"
                clearable
                @input="debouncedLoadUsers"
              />
            </template>
          </el-table-column>

          <!-- Plugin columns -->
          <el-table-column
            v-for="col in pluginTableColumns"
            :key="col.id"
            :label="col.label"
            :width="col.width"
          >
            <template #default="{ row }">
              <span v-html="col.render(row as any)" />
            </template>
          </el-table-column>

          <el-table-column label="Roles" width="180">
            <template #default="{ row }">
              <el-tag
                v-for="role in row.roles"
                :key="role.id"
                size="small"
                style="margin-right: 4px; margin-bottom: 2px"
              >
                {{ role.name }}
              </el-tag>
              <span v-if="row.roles.length === 0" style="color: var(--el-text-color-placeholder)">—</span>
            </template>
          </el-table-column>

          <el-table-column prop="isActive" label="Active" width="80" align="center">
            <template #default="{ row }">
              <el-switch
                :model-value="row.isActive"
                size="small"
                @click.prevent.stop
                @change="toggleActive(row)"
              />
            </template>
          </el-table-column>

          <el-table-column label="Created" width="120">
            <template #default="{ row }">
              <span style="font-size: 13px; color: var(--el-text-color-secondary)">
                {{ new Date(row.createdAt).toLocaleDateString() }}
              </span>
            </template>
          </el-table-column>

          <el-table-column label="Actions" width="120" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" size="small" text @click="openEditDialog(row)">
                Edit
              </el-button>
              <el-popconfirm
                title="Delete this user?"
                confirm-button-text="Delete"
                confirm-button-type="danger"
                @confirm="handleDelete(row)"
              >
                <template #reference>
                  <el-button type="danger" size="small" text>
                    Delete
                  </el-button>
                </template>
              </el-popconfirm>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- Mobile Card View -->
      <div v-if="users.length > 0 && !loading" class="mobile-cards">
        <div v-for="user in users" :key="user.id" class="user-card">
          <div class="user-card__header">
            <el-avatar :size="36" :src="user.avatarUrl">
              {{ user.displayName?.[0] ?? user.username[0] }}
            </el-avatar>
            <div class="user-card__title">
              <span class="user-card__name">{{ user.displayName ?? user.username }}</span>
              <span class="user-card__email">{{ user.email }}</span>
            </div>
            <el-switch
              :model-value="user.isActive"
              size="small"
              @click.prevent.stop
              @change="toggleActive(user)"
            />
          </div>
          
          <div class="user-card__content">
            <div class="user-card__field">
              <span class="user-card__label">Username</span>
              <span class="user-card__value">{{ user.username }}</span>
            </div>
            <div class="user-card__field">
              <span class="user-card__label">Roles</span>
              <div class="user-card__roles">
                <el-tag
                  v-for="role in user.roles"
                  :key="role.id"
                  size="small"
                  style="margin-right: 4px; margin-bottom: 2px"
                >
                  {{ role.name }}
                </el-tag>
                <span v-if="user.roles.length === 0" style="color: var(--el-text-color-placeholder)">—</span>
              </div>
            </div>
            <div class="user-card__field">
              <span class="user-card__label">Created</span>
              <span class="user-card__value">{{ new Date(user.createdAt).toLocaleDateString() }}</span>
            </div>
          </div>

          <div class="user-card__actions">
            <el-button type="primary" size="small" @click="openEditDialog(user)">
              Edit
            </el-button>
            <el-popconfirm
              title="Delete this user?"
              confirm-button-text="Delete"
              confirm-button-type="danger"
              @confirm="handleDelete(user)"
            >
              <template #reference>
                <el-button type="danger" size="small">
                  Delete
                </el-button>
              </template>
            </el-popconfirm>
          </div>
        </div>
      </div>

      <div v-if="totalPages > 1" style="display: flex; justify-content: center; padding: 16px 0">
        <el-pagination
          v-model:current-page="currentPage"
          :page-size="take"
          :total="total"
          layout="prev, pager, next"
          background
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- Create / Edit Dialog -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEditing ? 'Edit User' : 'Add User'"
      width="90%"
      :style="{'--el-dialog-width': dialogWidth}"
      :close-on-click-modal="false"
      center
    >
      <el-form
        ref="formRef"
        :model="form"
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
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="handleSave">
          {{ saving ? 'Saving...' : isEditing ? 'Save Changes' : 'Create User' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { usePluginStore } from '@/stores/pluginStore'
import { useNotificationStore } from '@/stores/notificationStore'
import eventBus from '@/utils/eventBus'
import * as usersApi from '@/api/users'
import type { User } from '@/types'
import { Refresh, Plus } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const pluginStore = usePluginStore()
const notificationStore = useNotificationStore()

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

// Debounce filter search
let filterDebounceTimer: ReturnType<typeof setTimeout> | null = null
function debouncedLoadUsers() {
  if (filterDebounceTimer) clearTimeout(filterDebounceTimer)
  filterDebounceTimer = setTimeout(() => {
    skip.value = 0 // Reset to first page
    loadUsers()
  }, 300)
}

const currentPage = computed({
  get: () => Math.floor(skip.value / take) + 1,
  set: (page: number) => {
    skip.value = (page - 1) * take
  },
})

const totalPages = computed(() => Math.ceil(total.value / take))

// Responsive dialog width
const dialogWidth = computed(() => window.innerWidth < 768 ? '95%' : '520px')

// Plugin contributions
const pluginTableColumns = computed(() => pluginStore.tableColumns)

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

function openCreateDialog() {
  isEditing.value = false
  editingUserId.value = null
  form.value = { email: '', username: '', displayName: '', password: '', isActive: true }
  dialogVisible.value = true
}

function openEditDialog(user: User) {
  isEditing.value = true
  editingUserId.value = user.id
  form.value = {
    email: user.email,
    username: user.username,
    displayName: user.displayName ?? '',
    password: '',
    isActive: user.isActive,
  }
  dialogVisible.value = true
}

async function handleSave() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return

  saving.value = true
  try {
    if (isEditing.value && editingUserId.value) {
      const updateData: usersApi.UpdateUserRequest = {
        email: form.value.email,
        username: form.value.username,
        displayName: form.value.displayName || undefined,
        isActive: form.value.isActive,
      }
      await usersApi.updateUser(editingUserId.value, updateData)
      notificationStore.addNotification('User updated successfully', 'success')
    } else {
      await usersApi.createUser({
        email: form.value.email,
        username: form.value.username,
        displayName: form.value.displayName || undefined,
        password: form.value.password,
      })
      notificationStore.addNotification('User created successfully', 'success')
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
    await usersApi.updateUser(user.id, { isActive: !user.isActive })
    notificationStore.addNotification(
      `User ${user.isActive ? 'deactivated' : 'activated'} successfully`,
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
onMounted(() => loadUsers())

function handleSort() {
  // Future: server-side sorting
}

function handlePageChange(page: number) {
  currentPage.value = page
}

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

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .page__header {
    flex-direction: column;
    gap: 12px;
    align-items: flex-start;
  }

  .users-filters {
    flex-direction: column;
    gap: 8px;
  }

  .users-filters__item {
    width: 100%;
    min-width: auto;
  }

  .desktop-table {
    display: none;
  }
}

@media (min-width: 769px) {
  .mobile-cards {
    display: none;
  }
}

/* ===== Mobile Card View ===== */
.mobile-cards {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.user-card {
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  padding: 16px;
  background: var(--el-bg-color);
}

.user-card__header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.user-card__title {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.user-card__name {
  font-weight: 500;
  font-size: 15px;
}

.user-card__email {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.user-card__content {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 16px;
}

.user-card__field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.user-card__label {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  text-transform: uppercase;
}

.user-card__value {
  font-size: 14px;
}

.user-card__roles {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.user-card__actions {
  display: flex;
  gap: 8px;
  justify-content: flex-end;
}

/* Horizontal scroll wrapper for tables on mobile */
.table-wrapper {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}
</style>