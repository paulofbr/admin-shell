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

    <el-card shadow="never" class="install-card">
      <div class="install-card__content">
        <div>
          <div class="install-card__title">Instalar novo plugin</div>
          <div class="install-card__hint">
            Faz upload de um pacote <code>.adminshell-plugin.zip</code>. O backend valida o manifest,
            copia o plugin para a pasta de plugins, recarrega o loader e ativa o plugin.
          </div>
        </div>

        <div class="install-card__actions">
          <label class="file-picker" :class="{ 'file-picker--selected': selectedFile }">
            <input
              :key="fileInputKey"
              type="file"
              accept=".zip,application/zip,application/x-zip-compressed"
              @change="onFileChange"
            >
            <span>{{ selectedFile?.name || 'Escolher ficheiro ZIP' }}</span>
          </label>

          <el-checkbox v-model="activateAfterInstall">Ativar depois de instalar</el-checkbox>

          <el-button
            type="primary"
            :icon="Upload"
            :loading="installing"
            :disabled="!selectedFile"
            @click="installSelectedPlugin"
          >
            Instalar plugin
          </el-button>
        </div>
      </div>
    </el-card>

    <ListViewer title="Plugins" subtitle="Manage installed plugins — {{ plugins.length }} loaded">
      <ResponsiveGrid
        :data="plugins as unknown as GridRow[]"
        :columns="gridColumns"
        :loading="loading"
        empty-text="No plugins"
      >
        <template #cell-plugin="{ row }">
          <div style="display: flex; align-items: center; gap: 10px">
            <div
              style="
                width: 36px; height: 36px; border-radius: 10px;
                display: flex; align-items: center; justify-content: center;
                font-size: 16px; font-weight: 600;
              "
              :style="{ background: getStatusColor((row as any).status) + '20', color: getStatusColor((row as any).status) }"
            >
              {{ (row as any).name[0] }}
            </div>
            <div>
              <div style="font-weight: 500">{{ (row as any).name }}</div>
              <div style="font-size: 12px; color: var(--el-text-color-secondary)">
                v{{ (row as any).version }} · {{ (row as any).id }}
              </div>
            </div>
          </div>
        </template>

        <template #cell-status="{ row }">
          <el-tag :type="getStatusType((row as any).status)" size="small" effect="plain">
            {{ getStatusLabel((row as any).status) }}
          </el-tag>
        </template>

        <template #cell-dependencies="{ row }">
          <div v-if="(row as any).dependencies && (row as any).dependencies.length > 0">
            <el-tag
              v-for="dep in (row as any).dependencies"
              :key="dep.pluginId"
              size="small"
              :type="dep.isResolved ? '' : 'danger'"
              style="margin-right: 4px; margin-bottom: 2px"
            >
              {{ dep.pluginId }} {{ dep.version ?? dep.versionConstraint }}
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

        <template #cell-loaded="{ row }">
          <span style="font-size: 12px; color: var(--el-text-color-secondary)">
            {{ (row as any).loadedAt ? new Date((row as any).loadedAt).toLocaleString() : '—' }}
          </span>
        </template>

        <template #cell-actions="{ row }">
          <el-switch
            v-if="(row as any).status === 2 || (row as any).status === 4 || (row as any).status === 5 || (row as any).status === 6"
            :model-value="(row as any).status === 2 || (row as any).status === 4"
            :disabled="(row as any).status === 5"
            size="small"
            @change="togglePlugin(row as any)"
          />
          <span v-else style="font-size: 12px; color: var(--el-text-color-placeholder)">—</span>
        </template>
      </ResponsiveGrid>
    </ListViewer>

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
import { useRouter } from 'vue-router'
import { usePluginStore } from '@/stores/pluginStore'
import { useExtensionStore } from '@/stores/extensionStore'
import { useNotificationStore } from '@/stores/notificationStore'
import ListViewer from '@/components/common/ListViewer.vue'
import ResponsiveGrid, { type GridColumn, type GridRow } from '@/components/common/ResponsiveGrid.vue'
import { Refresh, Upload } from '@element-plus/icons-vue'
import * as pluginsApi from '@/api/plugins'

const pluginStore = usePluginStore()
const extensionStore = useExtensionStore()
const notificationStore = useNotificationStore()
const router = useRouter()

const loading = ref(false)
const installing = ref(false)
const activateAfterInstall = ref(true)
const selectedFile = ref<File | null>(null)
const fileInputKey = ref(0)
const plugins = computed(() => pluginStore.plugins)
const failedPlugins = computed(() =>
  pluginStore.plugins.filter((p) => p.status === 'failed' || p.errorMessage)
)

const gridColumns: GridColumn[] = [
  { id: 'plugin', label: 'Plugin', minWidth: '220' },
  { id: 'status', label: 'Status', width: '110' },
  { id: 'description', label: 'Description', prop: 'description', minWidth: '200' },
  { id: 'dependencies', label: 'Dependencies', width: '180' },
  { id: 'loaded', label: 'Loaded', width: '110' },
  { id: 'actions', label: 'Actions', width: '120', fixed: 'right' },
]

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
  await pluginStore.loadPluginManifests()
  loading.value = false
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

async function installSelectedPlugin() {
  if (!selectedFile.value) return

  installing.value = true
  try {
    const result = await pluginsApi.installPlugin(selectedFile.value, activateAfterInstall.value)

    await pluginStore.loadPluginManifests()
    await extensionStore.refresh()

    selectedFile.value = null
    fileInputKey.value += 1

    const action = result.activated ? 'instalado e ativado' : 'instalado'
    notificationStore.addNotification(`Plugin "${result.pluginName}" ${action}`, 'success')
  } catch (error: unknown) {
    const message = error instanceof Error ? error.message : 'Failed to install plugin'
    notificationStore.addNotification(message, 'error')
  } finally {
    installing.value = false
  }
}

async function togglePlugin(plugin: any) {
  try {
    const isActive = plugin.status === 2 || plugin.status === 4
    if (isActive) {
      await pluginStore.disablePlugin(plugin.id)
      await extensionStore.refresh()
      if (router.currentRoute.value.meta.pluginId === plugin.id) {
        await router.replace('/')
      }
      notificationStore.addNotification(`Plugin "${plugin.name}" disabled`, 'info')
    } else {
      await pluginStore.enablePlugin(plugin.id)
      await extensionStore.refresh()
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

.install-card { margin-bottom: 16px; }
.install-card__content { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.install-card__title { font-weight: 600; color: var(--el-text-color-primary); margin-bottom: 4px; }
.install-card__hint { font-size: 13px; color: var(--el-text-color-secondary); line-height: 1.5; }
.install-card__hint code { color: var(--el-color-primary); }
.install-card__actions { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.file-picker { position: relative; display: inline-flex; align-items: center; height: 32px; padding: 0 12px; border: 1px dashed var(--el-border-color); border-radius: 8px; color: var(--el-text-color-regular); background: var(--el-fill-color-light); cursor: pointer; }
.file-picker:hover { border-color: var(--el-color-primary); color: var(--el-color-primary); }
.file-picker--selected { border-color: var(--el-color-success); color: var(--el-color-success); }
.file-picker input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .page { padding: 0; }

  .page__header {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .install-card__content {
    flex-direction: column;
    align-items: stretch;
  }

  .install-card__actions {
    justify-content: flex-start;
  }
}
</style>