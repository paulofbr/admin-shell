<template>
  <div class="vscode-web-page">
    <div class="vscode-web-page__header">
      <h2 class="vscode-web-page__title">VS Code Web</h2>
      <el-button 
        type="primary" 
        :icon="FullScreen" 
        @click="openInNewTab"
        class="vscode-web-page__fullscreen-btn"
      >
        Open in New Tab
      </el-button>
    </div>

    <div class="vscode-web-page__container">
      <iframe
        v-if="vscodeUrl"
        :src="vscodeUrl"
        class="vscode-web-page__iframe"
        @load="onIframeLoad"
        @error="onIframeError"
        sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-downloads"
        referrerpolicy="no-referrer"
      />
      <el-empty 
        v-else 
        description="Loading VS Code Web..." 
        class="vscode-web-page__loading"
      >
        <el-button type="primary" @click="initializeEditor">Retry</el-button>
      </el-empty>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { FullScreen } from '@element-plus/icons-vue'

const vscodeUrl = ref<string | null>(null)
const loading = ref(true)

// VS Code Web URL for opening repositories
// Uses vscode.dev which supports GitHub/GitLab repos via URL params
const projectRepository = computed(() => {
  // This can be configured via environment variables or settings
  return import.meta.env.VITE_VSCODE_PROJECT_REPO || 'https://github.com/microsoft/vscode'
})

function initializeEditor() {
  loading.value = true
  
  // VS Code Web URL format for opening a repository
  // vscode.dev supports: ?vscode=? (URL encoded workspace)
  const repoUrl = encodeURIComponent(projectRepository.value)
  vscodeUrl.value = `https://vscode.dev?vscode=${repoUrl}`
  
  // Alternative: use code-server if available
  // const codeServerUrl = 'http://localhost:8080'
  // vscodeUrl.value = codeServerUrl
}

function onIframeLoad() {
  loading.value = false
}

function onIframeError() {
  loading.value = false
  vscodeUrl.value = null
}

function openInNewTab() {
  if (vscodeUrl.value) {
    window.open(vscodeUrl.value, '_blank', 'noopener,noreferrer')
  }
}

onMounted(() => {
  initializeEditor()
})
</script>

<style scoped>
.vscode-web-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 24px;
  box-sizing: border-box;
}

.vscode-web-page__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}

.vscode-web-page__title {
  margin: 0;
  font-size: 20px;
  font-weight: 600;
}

.vscode-web-page__container {
  flex: 1;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
  min-height: 500px;
}

.vscode-web-page__iframe {
  width: 100%;
  height: 100%;
  border: none;
  display: block;
  min-height: 500px;
}

.vscode-web-page__loading {
  height: 100%;
  min-height: 500px;
}
</style>