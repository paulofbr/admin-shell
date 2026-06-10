<template>
  <div :class="['plugin-slot', `plugin-slot--${name}`]">
    <template v-if="slotWidgets.length === 0">
      <slot name="fallback">
        <div v-if="fallback" class="plugin-slot__fallback">{{ fallback }}</div>
      </slot>
    </template>
    <template v-for="widget in slotWidgets" :key="widget.id">
      <PluginWidget :widget="widget" />
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { usePluginStore } from '@/stores/pluginStore'
import PluginWidget from './PluginWidget.vue'

const props = defineProps<{
  name: string
  fallback?: string
}>()

const pluginStore = usePluginStore()

const slotWidgets = computed(() =>
  pluginStore.widgets.filter((w) => w.zone === props.name)
)
</script>