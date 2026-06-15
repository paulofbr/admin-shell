<template>
  <Teleport to="head">
    <template v-for="resource in headResources" :key="resourceKey(resource, 'head')">
      <link
        v-if="resource.type === 'style'"
        rel="stylesheet"
        :href="resource.src"
        data-plugin-resource="true"
      >
      <component
        v-else-if="resource.type === 'script'"
        :is="resource.type"
        :src="resource.src"
        data-plugin-resource="true"
      />
    </template>
  </Teleport>

  <Teleport to="body">
    <template v-for="resource in bodyResources" :key="resourceKey(resource, 'body')">
      <link
        v-if="resource.type === 'style'"
        rel="stylesheet"
        :href="resource.src"
        data-plugin-resource="true"
      >
      <component
        v-else-if="resource.type === 'script'"
        :is="resource.type"
        :src="resource.src"
        data-plugin-resource="true"
      />
    </template>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useExtensionStore, type PageResourceDescriptor } from '@/stores/extensionStore'

const route = useRoute()
const extensionStore = useExtensionStore()

const currentPath = computed(() => route.path)

const routeResources = computed(() =>
  extensionStore.getPageResourcesForRoute(currentPath.value),
)

const headResources = computed(() =>
  routeResources.value.filter(resource => resource.position !== 'body'),
)

const bodyResources = computed(() =>
  routeResources.value.filter(resource => resource.position === 'body'),
)

function resourceKey(resource: PageResourceDescriptor, position: string) {
  return `${position}:${resource.type}:${resource.src}`
}
</script>
