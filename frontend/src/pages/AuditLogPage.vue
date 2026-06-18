<template>
  <ListViewer title="Audit Log" :subtitle="auditSubtitle">
    <template #actions>
      <el-button :icon="Refresh" round @click="loadAuditLog" :loading="loading">
        Refresh
      </el-button>
    </template>

    <template #before-card>
      <el-alert v-if="error" :title="error" type="error" show-icon closable />
    </template>

    <template #toolbar>
      <div class="audit-filters">
        <el-select v-model="actionFilter" clearable placeholder="Filter by action" @change="applyFilter">
          <el-option v-for="a in actions" :key="a" :label="a" :value="a" />
        </el-select>
        <el-select v-model="userFilter" clearable placeholder="Filter by user" @change="applyFilter">
          <el-option v-for="u in users" :key="u" :label="u" :value="u" />
        </el-select>
        <span class="audit-filters__count">
          {{ total }} entries
        </span>
      </div>
    </template>

    <ResponsiveGrid
      :data="auditEntries as unknown as GridRow[]"
      :columns="gridColumns"
      :loading="loading"
      :total="total"
      :current-page="currentPage"
      :page-size="pageSize"
      empty-text="No audit entries yet"
      @page-change="changePage"
    >
      <template #cell-action="{ row }">
        <el-tag :type="actionType((row as unknown as AuditEntry).action)" size="small">
          {{ (row as unknown as AuditEntry).action }}
        </el-tag>
      </template>

      <template #cell-timestamp="{ row }">
        {{ formatDate((row as unknown as AuditEntry).timestamp) }}
      </template>
    </ResponsiveGrid>
  </ListViewer>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import ListViewer from '@admin-shell/ui/ListViewer.vue'
import ResponsiveGrid from '@admin-shell/ui/ResponsiveGrid.vue'
import type { AuditEntry } from '@/api/audit'
import type { GridColumn, GridRow } from '@admin-shell/ui/types'
import { getAuditLog } from '@/api/audit'

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

const auditSubtitle = computed(() => `System activity and user audit trail — ${total.value} entries`)

const gridColumns: GridColumn[] = [
  { id: 'action', label: 'Action', prop: 'action', width: '160' },
  { id: 'entityType', label: 'Type', prop: 'entityType', width: '120' },
  { id: 'entityId', label: 'Entity ID', prop: 'entityId', width: '180' },
  { id: 'performedBy', label: 'Performed By', prop: 'performedBy', width: '180' },
  { id: 'details', label: 'Details', prop: 'details', minWidth: '200' },
  { id: 'ipAddress', label: 'IP', prop: 'ipAddress', width: '150' },
  { id: 'timestamp', label: 'Timestamp', prop: 'timestamp', width: '180' },
]

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
    const data = actionFilter.value
      ? await getAuditLog({ skip, take: pageSize, action: actionFilter.value })
      : await getAuditLog({ skip, take: pageSize })

    auditEntries.value = data.data
    total.value = data.total

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
.audit-filters {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  min-width: 0;
}

.audit-filters .el-select {
  width: 200px;
  max-width: 100%;
}

.audit-filters__count {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.audit-pagination {
  display: flex;
  justify-content: center;
  margin-top: 16px;
}

.audit-loading-footer {
  padding: 60px 0;
  text-align: center;
}

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .audit-filters {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }

  .audit-filters .el-select {
    width: 100% !important;
  }
}
</style>