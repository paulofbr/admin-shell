<template>
  <div
    class="plugin-widget"
    :style="{
      width: widget.width ? `${widget.width}px` : '100%',
      height: widget.height ? `${widget.height}px` : 'auto',
    }"
  >
    <div class="plugin-widget__header">
      <h4 class="plugin-widget__title">{{ widget.title }}</h4>
      <button
        class="btn btn--sm btn--ghost"
        :disabled="isRefreshing"
        @click="handleRefresh"
      >
        {{ isRefreshing ? '⟳' : '↻' }}
      </button>
    </div>
    <div class="plugin-widget__content">
      <div v-if="isRefreshing" class="plugin-widget__loading">
        <div class="spinner spinner--sm" />
      </div>
      <div v-else class="plugin-widget__body">
        <p class="plugin-widget__placeholder">
          Widget content for "{{ widget.title }}"
        </p>
        <pre v-if="widget.settings" class="plugin-widget__settings">{{ JSON.stringify(widget.settings, null, 2) }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { WidgetDescriptor } from '@/types'

defineProps<{
  widget: WidgetDescriptor
}>()

const isRefreshing = ref(false)

function handleRefresh() {
  isRefreshing.value = true
  setTimeout(() => {
    isRefreshing.value = false
  }, 1000)
}
</script>