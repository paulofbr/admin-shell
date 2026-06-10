<template>
  <router-view />
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { usePluginStore } from '@/stores/pluginStore'

const authStore = useAuthStore()
const pluginStore = usePluginStore()

onMounted(async () => {
  pluginStore.initEventListeners()
  await authStore.loadFromStorage()
  if (authStore.isAuthenticated) {
    await pluginStore.loadPluginManifests()
  }
})
</script>