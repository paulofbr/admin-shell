<template>
  <div class="app-layout" :class="{ 'app-layout--collapsed': sidebarCollapsed }">
    <Sidebar
      :is-open="sidebarOpen"
      :is-collapsed="sidebarCollapsed"
      @close="sidebarOpen = false"
      @toggleCollapse="sidebarCollapsed = !sidebarCollapsed"
    />
    <div class="app-layout__main">
      <Navbar @menu-toggle="sidebarOpen = !sidebarOpen" />
      <main class="app-layout__content">
        <router-view v-slot="{ Component }" :key="renderKey">
          <component :is="Component" :key="renderKey" />
        </router-view>
      </main>
    </div>
    <PageResources />
    <NotificationContainer />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useExtensionStore } from '@/stores/extensionStore'
import Sidebar from './Sidebar.vue'
import Navbar from './Navbar.vue'
import NotificationContainer from '@/components/common/NotificationContainer.vue'
import PageResources from '@/components/common/PageResources.vue'

const route = useRoute()
const extensionStore = useExtensionStore()
const sidebarOpen = ref(false)
const sidebarCollapsed = ref(false)
const renderKey = ref(0)

watch(
  () => route.fullPath,
  () => {
    renderKey.value++
  },
)

onMounted(async () => {
  // Load plugin extensions on mount
  await extensionStore.loadExtensions()
})
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
}

.app-layout--collapsed .app-layout__main {
  margin-left: 64px;
}

.app-layout__main {
  margin-left: 240px;
  transition: margin-left 0.25s ease;
}

@media (max-width: 768px) {
  .app-layout__main {
    margin-left: 0;
  }
}

.app-layout__content {
  min-height: calc(100vh - 56px);
  background: var(--el-bg-color);
  overflow-x: hidden;
}

@media (max-width: 768px) {
  .app-layout__content {
    max-width: 100%;
  }
}
</style>