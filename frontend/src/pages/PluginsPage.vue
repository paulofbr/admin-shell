<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Plugins</h2>
        <p class="page__subtitle">Manage installed plugins — {{ plugins.length }} loaded</p>
      </div>
      <el-button :icon="Refresh" round @click="load" :loading="loading">
        Refresh
      </el-button>
    </div>

    <el-card shadow="never">
      <div class="table-wrapper">
        <el-table v-loading="loading" :data="plugins" stripe style="width: 100%">
          <el-table-column label="Plugin" min-width="220">
          <template #default="{ row }">
            <div style="display: flex; align-items: center; gap: 10px">
              <div
                style="
                  width: 36px; height: 36px; border-radius: 10px;
                  display: flex; align-items: center; justify-content: center;
                  font-size: 16px; font-weight: 600;
                "
                :style="{ background: getStatusColor(row.status) + '20', color: getStatusColor(row.status) }"
              >
                {{ row.name[0] }}
              </div>
              <div>
                <div style="font-weight: 500">{{ row.name }}</div>
                <div style="font-size: 12px; color: var(--el-text-color-secondary)">
                  v{{ row.version }} · {{ row.id }}
                </div>
              </div>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="Status" width="110">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small" effect="plain">
              {{ getStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="description" label="Description" min-width="200">
          <template #default="{ row }">
            <span style="font-size: 13px; color: var(--el-text-color-secondary)">
              {{ row.description || 'No description' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column label="Dependencies" width="180">
          <template #default="{ row }">
            <div v-if="row.dependencies && row.dependencies.length > 0">
              <el-tag
                v-for="dep in row.dependencies"
                :key="dep.pluginId"
                size="small"
                :type="dep.isResolved ? '' : 'danger'"
                style="margin-right: 4px; margin-bottom: 2px"
              >
                {{ dep.pluginId }}
                <el-tooltip
                  v-if="!dep.isResolved"
                  :content="dep.errorMessage || 'Unresolved dependency'"
                >
                  <span style="cursor: help">⚠️</span>
                </el-tooltip>
              </el-tag>
            </div>
            <span v-else style="font-size: 12px; color: var(--el-text-color-placeholder)">None</span>
          </template>
        </el-table-column>

        <el-table-column label="Loaded" width="110">
          <template #default="{ row }">
            <span style="font-size: 12px; color: var(--el-text-color-secondary)">
              {{ row.loadedAt ? new Date(row.loadedAt).toLocaleString() : '—' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column label="Actions" width="120" fixed="right">
          <template #default="{ row }">
            <el-switch
              v-if="row.status === 2 || row.status === 4 || row.status === 5 || row.status === 6"
              :model-value="row.status === 2 || row.status === 4"
              :disabled="row.status === 5"
              size="small"
              @change="togglePlugin(row)"
            />
            <span v-else style="font-size: 12px; color: var(--el-text-color-placeholder)">—</span>
          </template>
        </el-table-column>
      </el-table>
      </div>
    </el-card>

    <!-- Error details for failed plugins -->
    <el-card v-if="failedPlugins.length > 0" shadow="never" style="margin-top: 16px">
      <template #header>
        <span style="color: var(--el-color-danger); font-weight: 500">
          ⚠ Failed Plugins ({{ failedPlugins.length }})
        </span>
      </template>
      <div v-for="p in failedPlugins" :key="p.id" style="margin-bottom: 12px">
        <div style="font-weight: 500; margin-bottom: 4px">{{ p.name }} ({{ p.id }})</div>
        <el-alert :title="p.errorMessage" type="error" show-icon :closable="false" />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePluginStore } from '@/stores/pluginStore'
import { useNotificationStore } from '@/stores/notificationStore'
import { Refresh } from '@element-plus/icons-vue'

const pluginStore = usePluginStore()
const notificationStore = useNotificationStore()

const loading = ref(false)
const plugins = computed(() => pluginStore.plugins)
const failedPlugins = computed(() =>
  pluginStore.plugins.filter((p) => p.status === 'failed' || p.errorMessage)
)

function getStatusColor(status: number | string): string {
  const map: Record<string, string> = {
    '0': '#909399', // discovered
    '1': '#e6a23c', // loading
    '2': '#67c23a', // loaded/active
    '3': '#409eff', // initializing
    '4': '#67c23a', // active
    '5': '#f56c6c', // failed
    '6': '#909399', // disabled
  }
  return map[String(status)] ?? '#909399'
}

function getStatusType(status: number | string): string {
  const map: Record<string, string> = {
    '0': 'info',
    '1': 'warning',
    '2': 'success',
    '3': 'primary',
    '4': 'success',
    '5': 'danger',
    '6': 'info',
  }
  return map[String(status)] ?? 'info'
}

function getStatusLabel(status: number | string): string {
  const map: Record<string, string> = {
    '0': 'Discovered',
    '1': 'Loading',
    '2': 'Loaded',
    '3': 'Initializing',
    '4': 'Active',
    '5': 'Failed',
    '6': 'Disabled',
  }
  return map[String(status)] ?? 'Unknown'
}

async function load() {
  loading.value = true
  await pluginStore.loadPlugins()
  loading.value = false
}

async function togglePlugin(plugin: any) {
  try {
    const isActive = plugin.status === 2 || plugin.status === 4
    if (isActive) {
      await pluginStore.disablePlugin(plugin.id)
      notificationStore.addNotification(`Plugin "${plugin.name}" disabled`, 'info')
    } else {
      await pluginStore.enablePlugin(plugin.id)
      notificationStore.addNotification(`Plugin "${plugin.name}" enabled`, 'success')
    }
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to toggle plugin'
    notificationStore.addNotification(message, 'error')
  }
}
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
}

/* Horizontal scroll wrapper for tables on mobile */
.table-wrapper {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}
</style>