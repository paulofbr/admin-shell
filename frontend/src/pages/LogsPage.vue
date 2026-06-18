<template>
  <ListViewer title="Application Logs" :subtitle="subtitle">
    <template #before-card>
      <el-alert v-if="error" :title="error" type="error" show-icon closable />
      <el-alert v-if="warning" :title="warning" type="warning" show-icon closable />
    </template>

    <ResponsiveGrid
      :data="logEntries as unknown as GridRow[]"
      :columns="gridColumns"
      :loading="loading"
      :total="gridTotal"
      :current-page="currentPage"
      :page-size="pageSize"
      empty-text="No log entries found"
      @page-change="changePage"
    >
      <template #toolbar>
        <div class="logs-filters">
          <el-button :icon="Refresh" round @click="reload" :loading="loading">
            Refresh
          </el-button>

          <el-select
            v-model="typeFilter"
            clearable
            placeholder="Filter by type"
            class="logs-filters__type"
            @change="applyFilters"
          >
            <el-option v-for="level in levels" :key="level" :label="level" :value="level" />
          </el-select>

          <el-input
            v-model.trim="messageFilter"
            clearable
            placeholder="Filter by message"
            class="logs-filters__message"
            @keyup.enter="applyFilters"
          >
            <template #append>
              <el-button @click="applyFilters">Search</el-button>
            </template>
          </el-input>

          <span class="logs-filters__meta">
            {{ metaText }}
          </span>
        </div>
      </template>

      <template #cell-level="{ row }">
        <el-tag :type="levelType((row as unknown as LogEntry).level)" size="small">
          {{ (row as unknown as LogEntry).level }}
        </el-tag>
      </template>

      <template #cell-timestamp="{ row }">
        {{ formatDate((row as unknown as LogEntry).timestamp) }}
      </template>

      <template #cell-message="{ row }">
        <span class="logs-grid__message">{{ (row as unknown as LogEntry).message }}</span>
      </template>

      <template #cell-exception="{ row }">
        <el-text v-if="(row as unknown as LogEntry).exception" type="danger" truncated>
          {{ (row as unknown as LogEntry).exception }}
        </el-text>
      </template>
    </ResponsiveGrid>
  </ListViewer>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import ListViewer from '@admin-shell/ui/ListViewer.vue'
import ResponsiveGrid from '@admin-shell/ui/ResponsiveGrid.vue'
import type { GridColumn, GridRow } from '@admin-shell/ui/types'
import { getLogLevels, getLogs, type LogEntry } from '@/api/logs'

const loading = ref(false)
const error = ref<string | null>(null)
const warning = ref<string | null>(null)
const logEntries = ref<LogEntry[]>([])
const levels = ref<string[]>([])
const typeFilter = ref('')
const messageFilter = ref('')
const currentPage = ref(1)
const pageSize = 50
const scannedBytes = ref(0)
const hasMore = ref(false)

const subtitle = computed(() => `Serilog file tail — ${logEntries.value.length} entries`)
const metaText = computed(() => {
  const scanned = scannedBytes.value
  return scanned > 0 ? `scanned ${formatBytes(scanned)}` : 'no data'
})

const gridTotal = computed(() => {
  if (!hasMore.value) {
    return (currentPage.value - 1) * pageSize + logEntries.value.length
  }

  return currentPage.value * pageSize
})

const gridColumns: GridColumn[] = [
  { id: 'timestamp', label: 'Timestamp', prop: 'timestamp', width: '190' },
  { id: 'level', label: 'Type', prop: 'level', width: '120' },
  { id: 'source', label: 'Source', prop: 'source', minWidth: '180' },
  { id: 'message', label: 'Message', prop: 'message', minWidth: '320' },
  { id: 'exception', label: 'Exception', prop: 'exception', minWidth: '220' },
]

function formatDate(timestamp: string | null): string {
  if (!timestamp) return ''

  const date = new Date(timestamp)
  if (Number.isNaN(date.getTime())) return timestamp

  return date.toLocaleString('pt-PT', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function levelType(level: string): string {
  switch (level.toLowerCase()) {
    case 'fatal':
    case 'error':
      return 'danger'
    case 'warning':
      return 'warning'
    case 'information':
      return 'success'
    default:
      return 'info'
  }
}

async function loadLevels() {
  try {
    levels.value = await getLogLevels()
  } catch {
    levels.value = ['Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Fatal']
  }
}

async function reload() {
  await loadLogs()
}

async function applyFilters() {
  currentPage.value = 1
  await loadLogs()
}

function changePage(page: number) {
  currentPage.value = page
  loadLogs()
}

async function loadLogs() {
  loading.value = true
  error.value = null
  warning.value = null

  try {
    const data = await getLogs({
      skip: (currentPage.value - 1) * pageSize,
      take: pageSize,
      type: typeFilter.value || undefined,
      message: messageFilter.value || undefined,
    })

    logEntries.value = data.data
    if (currentPage.value > 1 && data.data.length === 0 && !data.hasMore) {
      currentPage.value = 1
    }
    scannedBytes.value = data.scannedBytes
    hasMore.value = data.hasMore
    warning.value = data.warning
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Unknown error'
    logEntries.value = []
    scannedBytes.value = 0
    hasMore.value = false
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadLevels()
  loadLogs()
})
</script>

<style scoped>
.logs-filters {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
  min-width: 0;
}

.logs-filters__type {
  width: 180px;
  max-width: 100%;
}

.logs-filters__message {
  width: 320px;
  max-width: 100%;
}

.logs-filters__meta {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.logs-grid__message {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .logs-filters {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }

  .logs-filters__type,
  .logs-filters__message {
    width: 100% !important;
  }
}
</style>
