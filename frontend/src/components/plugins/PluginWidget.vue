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
        <component
          :is="registeredComponent"
          v-if="registeredComponent"
          :widget="widget"
          :plugin-id="pluginId"
        />
        <template v-else>
          <p class="plugin-widget__placeholder">
            Widget content for "{{ widget.title }}"
          </p>
          <pre v-if="widget.settings" class="plugin-widget__settings">{{ JSON.stringify(widget.settings, null, 2) }}</pre>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, provide, ref } from 'vue'
import type { Component } from 'vue'
import type { WidgetDescriptor } from '@/stores/extensionStore'
import { getPluginComponentOwner, resolvePluginComponent } from '@/utils/pluginComponentRegistry'
import { getPluginServices } from '@/utils/pluginServices'

const props = defineProps<{
  widget: WidgetDescriptor
}>()

const isRefreshing = ref(false)
const registeredComponent = computed<Component | undefined>(() =>
  props.widget.componentName
    ? resolvePluginComponent(props.widget.componentName) as Component | undefined
    : undefined,
)

const pluginId = computed(() =>
  props.widget.componentName
    ? getPluginComponentOwner(props.widget.componentName)
    : undefined,
)

const pluginServices = computed(() =>
  pluginId.value ? getPluginServices(pluginId.value) : undefined,
)

if (pluginId.value && pluginServices.value) {
  provide(`admin-shell:plugin-services:${pluginId.value}`, pluginServices.value)
}

function handleRefresh() {
  isRefreshing.value = true
  setTimeout(() => {
    isRefreshing.value = false
  }, 1000)
}
</script>
