<template>
  <div class="plugin-tab-content">
    <component
      :is="registeredComponent"
      v-if="registeredComponent"
      :tab="tab"
    />

    <div v-else-if="tab.componentPath === 'UserActivityTab'" class="audit-log">
      <div v-if="loading" class="plugin-tab-content__loading">
        <el-skeleton :rows="5" animated />
      </div>
      <el-empty v-else-if="entries.length === 0" description="No audit entries found" />
      <el-timeline v-else>
        <el-timeline-item
          v-for="entry in entries"
          :key="entry.timestamp"
          :timestamp="formatTimestamp(entry.timestamp)"
          placement="top"
        >
          <el-card shadow="never">
            <strong>{{ entry.action || entry.description }}</strong>
            <p>{{ entry.user }}</p>
          </el-card>
        </el-timeline-item>
      </el-timeline>
    </div>

    <el-empty v-else :description="`${tab.label} is ready for plugin rendering`" />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElEmpty, ElSkeleton, ElTimeline, ElTimelineItem, ElCard } from 'element-plus'
import type { Component } from 'vue'
import { authApi } from '@/services/api'
import { resolvePluginComponent } from '@admin-shell/ui/plugin-component-registry'
import type { TabDescriptor } from '@/stores/extensionStore'

const props = defineProps<{
  tab: TabDescriptor
}>()

const loading = ref(false)
const entries = ref<any[]>([])

const registeredComponent = computed<Component | undefined>(() =>
  resolvePluginComponent(props.tab.componentPath) as Component | undefined,
)

async function loadAuditEntries() {
  loading.value = true
  try {
    const response = await authApi.get('/api/plugins/useraudit/audit')
    entries.value = response.data?.Data ?? []
  } catch {
    entries.value = []
  } finally {
    loading.value = false
  }
}

function formatTimestamp(value: string | number | Date) {
  return new Date(value).toLocaleString()
}

onMounted(() => {
  if (props.tab.componentPath === 'UserActivityTab') {
    loadAuditEntries()
  }
})
</script>

<style scoped>
.plugin-tab-content {
  max-width: 720px;
}

.plugin-tab-content__loading {
  padding: 8px 0;
}
</style>
