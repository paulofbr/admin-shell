<template>
  <div class="header-actions">
    <template v-for="action in actions" :key="action.id">
      <el-button
        v-if="action.actionType === 'route'"
        :icon="getIcon(action.icon)"
        :type="getButtonType(action.actionType)"
        size="small"
        class="header-actions__btn"
        @click="router.push(action.actionValue ?? '/')"
      >
        {{ action.label }}
      </el-button>

      <el-button
        v-else-if="action.actionType === 'modal'"
        :icon="getIcon(action.icon)"
        :type="getButtonType(action.actionType)"
        size="small"
        class="header-actions__btn"
        @click="openModal(action)"
      >
        {{ action.label }}
      </el-button>

      <el-popconfirm
        v-else-if="action.actionType === 'api'"
        :title="`Execute: ${action.label}?`"
        @confirm="callApi(action)"
      >
        <template #reference>
          <el-button :icon="getIcon(action.icon)" :type="action.actionType" size="small" class="header-actions__btn">
            {{ action.label }}
          </el-button>
        </template>
      </el-popconfirm>

      <el-tooltip
        v-else-if="action.actionType === 'emit'"
        :content="action.label"
        placement="bottom"
      >
        <el-button
          :icon="getIcon(action.icon)"
          :type="getButtonType(action.actionType)"
          size="small"
          circle
          class="header-actions__btn"
          @click="emitEvent(action)"
        />
      </el-tooltip>

      <el-button
        v-else
        :icon="getIcon(action.icon)"
        :type="getButtonType(action.actionType)"
        size="small"
        class="header-actions__btn"
        @click="openModal(action)"
      >
        {{ action.label }}
      </el-button>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useExtensionStore, type HeaderActionDescriptor } from '@/stores/extensionStore'
import { ElMessage } from 'element-plus'
import { authApi } from '@/services/api'
import { Plus, Download, Edit, Delete, Share, Refresh, Setting } from '@element-plus/icons-vue'

const props = defineProps<{
  target: string
  targetPage?: string
}>()

const router = useRouter()
const extensionStore = useExtensionStore()

const actions = computed(() =>
  extensionStore.getHeaderActions(props.target, props.targetPage),
)

const iconMap: Record<string, any> = {
  plus: Plus,
  download: Download,
  edit: Edit,
  delete: Delete,
  share: Share,
  refresh: Refresh,
  setting: Setting,
}

function getIcon(name?: string): any {
  if (!name) return undefined
  const key = name.toLowerCase().replace(/[^a-z0-9]/g, '')
  return iconMap[key] || undefined
}

function getButtonType(actionType: string) {
  return actionType === 'primary' ? 'primary' : 'default'
}

function openModal(action: HeaderActionDescriptor) {
  ElMessage.info(`Action: ${action.label} (value: ${action.actionValue ?? '-'})`)
}

async function callApi(action: HeaderActionDescriptor) {
  try {
    if (!action.actionValue) return
    await authApi.post(action.actionValue)
    ElMessage.success(`${action.label} completed`)
  } catch (e: any) {
    ElMessage.error(`Action failed: ${e?.message || 'Unknown error'}`)
  }
}

function emitEvent(action: HeaderActionDescriptor) {
  window.dispatchEvent(new CustomEvent('plugin-action', {
    detail: { actionId: action.id, value: action.actionValue },
  }))
  ElMessage.info(`Event emitted: ${action.actionValue}`)
}
</script>

<style scoped>
.header-actions {
  display: flex;
  align-items: center;
  gap: 6px;
}

.header-actions__btn {
  --el-button-size: 28px;
}
</style>
