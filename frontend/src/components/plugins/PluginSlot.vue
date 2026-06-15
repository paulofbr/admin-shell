<template>
  <div v-if="widgets.length === 0" class="plugin-slot__empty">
    <el-empty :description="emptyText" :image-size="60" />
  </div>
  <el-row v-else :gutter="16" class="plugin-slot">
    <el-col
      v-for="widget in widgets"
      :key="widget.id"
      :xs="24"
      :md="widget.width >= 6 ? 12 : 24"
      :lg="widget.width >= 8 ? 8 : 12"
    >
      <PluginWidget :widget="widget" />
    </el-col>
  </el-row>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ElEmpty } from 'element-plus'
import { useExtensionStore } from '@/stores/extensionStore'
import PluginWidget from '@/components/plugins/PluginWidget.vue'
import type { WidgetDescriptor } from '@/types'

const props = withDefaults(defineProps<{
  zone: string
  emptyText?: string
}>(), {
  emptyText: 'No plugin widgets contributed',
})

const extensionStore = useExtensionStore()

const widgets = computed<WidgetDescriptor[]>(() =>
  extensionStore.getWidgetsByZone(props.zone),
)
</script>

<style scoped>
.plugin-slot__empty {
  padding: 12px 0;
}
</style>
