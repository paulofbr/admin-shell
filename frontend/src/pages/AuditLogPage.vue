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
    <ListViewer title="Audit Log" subtitle="System activity and user audit trail">
      <template #toolbar>
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
      </template>

      <ResponsiveGrid
        :data="auditEntries as unknown as GridRow[]"
        :columns="gridColumns"
        :loading="loading"
        empty-text="No audit entries yet"
      >
        <template #cell-action="{ row }">
          <el-tag :type="actionType((row as unknown as AuditEntry).action)" size="small">{{ (row as unknown as AuditEntry).action }}</el-tag>
        </template>

        <template #cell-timestamp="{ row }">
          {{ formatDate((row as unknown as AuditEntry).timestamp) }}
        </template>
      </ResponsiveGrid>

      <template #footer="{ loading: gridLoading }">
        <div v-if="total > pageSize" style="margin-top: 16px; display: flex; justify-content: center">
          <el-pagination
            v-model:current-page="currentPage"
            :page-size="pageSize"
            :total="total"
            layout="prev, pager, next"
            @current-change="changePage"
          />
        </div>
        <div v-if="gridLoading" style="text-align: center; padding: 60px 0">
          <el-skeleton :rows="6" animated />
        </div>
      </template>
    </ListViewer>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import ListViewer from '@/components/common/ListViewer.vue'
import ResponsiveGrid, { type GridColumn, type GridRow } from '@/components/common/ResponsiveGrid.vue'
import { getAuditLog, type AuditEntry } from '@/api/audit'

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
.page { padding: 24px; }
.page__header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
.page__title { margin: 0; font-size: 22px; font-weight: 600; color: var(--el-text-color-primary); }
.page__subtitle { margin: 4px 0 0; font-size: 14px; color: var(--el-text-color-secondary); }

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .page { padding: 0; }

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
}
</style>