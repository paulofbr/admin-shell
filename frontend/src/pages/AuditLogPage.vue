<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Audit Log</h2>
        <p class="page__subtitle">System activity and user audit trail</p>
      </div>
      <el-button :icon="Refresh" round @click="loadAuditLog">Refresh</el-button>
    </div>

    <!-- Loading -->
    <div v-if="loading" style="text-align: center; padding: 60px 0">
      <el-skeleton :rows="6" animated />
    </div>

    <!-- Error -->
    <el-alert v-else-if="error" :title="error" type="error" show-icon closable />

    <!-- Content -->
    <el-card v-else shadow="never">
      <!-- Filters -->
      <div style="display: flex; gap: 12px; margin-bottom: 16px; flex-wrap: wrap; align-items: center" class="audit-filters">
        <el-select v-model="actionFilter" clearable placeholder="Filter by action" style="width: 200px" @change="applyFilter">
          <el-option v-for="a in actions" :key="a" :label="a" :value="a" />
        </el-select>
        <el-select v-model="userFilter" clearable placeholder="Filter by user" style="width: 200px" @change="applyFilter">
          <el-option v-for="u in users" :key="u" :label="u" :value="u" />
        </el-select>
        <span style="font-size: 13px; color: #909399">
          {{ total }} entries
        </span>
      </div>

      <!-- Table -->
      <div class="table-wrapper">
        <el-table :data="auditEntries" stripe style="width: 100%" empty-text="No audit entries yet">
          <el-table-column prop="action" label="Action" width="160">
            <template #default="{ row }">
              <el-tag :type="actionType(row.action)" size="small">{{ row.action }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="entityType" label="Type" width="120" />
          <el-table-column prop="entityId" label="Entity ID" width="180" :show-overflow-tooltip="true" />
          <el-table-column prop="performedBy" label="Performed By" width="180" />
          <el-table-column prop="details" label="Details" min-width="200" :show-overflow-tooltip="true" />
          <el-table-column prop="ipAddress" label="IP" width="150" />
          <el-table-column prop="timestamp" label="Timestamp" width="180">
            <template #default="{ row }">
              {{ formatDate(row.timestamp) }}
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- Pagination -->
      <div v-if="total > pageSize" style="margin-top: 16px; display: flex; justify-content: center">
        <el-pagination
          v-model:current-page="currentPage"
          :page-size="pageSize"
          :total="total"
          layout="prev, pager, next"
          @current-change="changePage"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Refresh } from '@element-plus/icons-vue'

interface AuditEntry {
  id?: string
  action: string
  entityType: string
  entityId: string | null
  performedBy: string
  details: string | null
  ipAddress: string | null
  timestamp: string
}

const API_BASE = '/api'

const loading = ref(false)
const error = ref<string | null>(null)
const auditEntries = ref<AuditEntry[]>([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = 50
const actionFilter = ref('')
const userFilter = ref('')
const actions = ref<string[]>([])
const users = ref<string[]>([])

function formatDate(ts: string): string {
  return new Date(ts).toLocaleString('pt-PT', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function actionType(action: string): string {
  if (action.startsWith('LOGIN')) return 'success'
  if (action.startsWith('LOGOUT')) return 'info'
  if (action.startsWith('USER_CREATE') || action.startsWith('USER_REGISTER')) return ''
  if (action.startsWith('USER_UPDATE')) return 'warning'
  if (action.startsWith('USER_DELETE') || action.startsWith('LOGIN_FAILED')) return 'danger'
  return 'info'
}

async function loadAuditLog() {
  loading.value = true
  error.value = null

  try {
    const skip = (currentPage.value - 1) * pageSize
    const token = localStorage.getItem('auth_token')
    const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {}

    let url = `${API_BASE}/auditlog?skip=${skip}&take=${pageSize}`
    if (actionFilter.value) url = `${API_BASE}/auditlog/action/${actionFilter.value}?skip=${skip}&take=${pageSize}`

    const res = await fetch(url, { headers })
    if (!res.ok) throw new Error(`Failed to load audit log: ${res.status}`)

    const data = await res.json()
    auditEntries.value = data.data || []
    total.value = data.total || 0

    // Extract unique actions and users for filters
    if (!actionFilter.value) {
      const seen = new Set<string>()
      auditEntries.value.forEach((e) => seen.add(e.action))
      actions.value = Array.from(seen)
    }
    {
      const seen = new Set<string>()
      auditEntries.value.forEach((e) => seen.add(e.performedBy))
      users.value = Array.from(seen)
    }
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Unknown error'
  } finally {
    loading.value = false
  }
}

function applyFilter() {
  currentPage.value = 1
  loadAuditLog()
}

function changePage(page: number) {
  currentPage.value = page
  loadAuditLog()
}

onMounted(loadAuditLog)
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

  .audit-filters {
    width: 100%;
    flex-direction: column;
    gap: 8px;
  }

  .audit-filters .el-select {
    width: 100% !important;
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