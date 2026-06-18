<template>
  <div class="global-search" ref="searchRef">
    <el-input
      v-model="query"
      :placeholder="placeholder"
      :prefix-icon="Search"
      clearable
      class="global-search__input"
      @input="onSearch"
      @focus="onFocus"
      @keydown.esc="onClose"
    >
      <template #prefix>
        <el-icon class="global-search__icon"><Search /></el-icon>
      </template>
    </el-input>

    <!-- Results dropdown -->
    <Transition name="dropdown">
      <div v-if="showResults && (results.length > 0 || query.length > 0)" class="global-search__dropdown">
        <div v-if="loading" class="global-search__loading">
          <el-icon class="is-loading"><Loading /></el-icon>
          <span>Searching...</span>
        </div>

        <template v-else>
          <div v-if="results.length === 0 && query.length >= 2" class="global-search__empty">
            <el-empty :description="`No results for '${query}'`" :image-size="60" />
          </div>

          <div v-for="group in groupedResults" :key="group.category" class="global-search__group">
            <div class="global-search__group-label">{{ group.category }}</div>
            <div
              v-for="item in group.items"
              :key="item.title + item.url"
              class="global-search__item"
              @click="navigateTo(item)"
              @mouseenter="hoveredIndex = results.indexOf(item)"
            >
              <el-icon v-if="item.icon" class="global-search__item-icon">
                <component :is="getIconComponent(item.icon)" />
              </el-icon>
              <div class="global-search__item-content">
                <div class="global-search__item-title">{{ item.title }}</div>
                <div v-if="item.description" class="global-search__item-desc">{{ item.description }}</div>
                <div v-if="item.providerName" class="global-search__item-provider">{{ item.providerName }}</div>
              </div>
              <el-tag size="small" type="info" effect="plain">{{ item.score }}</el-tag>
            </div>
          </div>
        </template>

        <div v-if="results.length > 0" class="global-search__footer">
          <span class="global-search__footer-hint">
            <kbd>↑</kbd> <kbd>↓</kbd> navigate &middot; <kbd>Enter</kbd> select &middot; <kbd>Esc</kbd> close
          </span>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search, Loading } from '@element-plus/icons-vue'
import { authApi } from '@/services/api'
import type { Component } from 'vue'

const router = useRouter()
const searchRef = ref<HTMLElement | null>(null)
const query = ref('')
const results = ref<SearchResultItem[]>([])
const loading = ref(false)
const showResults = ref(false)
const hoveredIndex = ref(-1)
let debounceTimer: ReturnType<typeof setTimeout> | null = null
let searchAbortController: AbortController | null = null

export interface SearchResultItem {
  title: string
  description?: string
  url: string
  category: string
  icon?: string
  score: number
  providerId?: string
  providerName?: string
}

const placeholder = computed(() => {
  if (query.value) return `Search "${query.value}"...`
  return 'Search users, roles, settings, audit... (Ctrl+K)'
})

interface ResultGroup {
  category: string
  items: SearchResultItem[]
}

const groupedResults = computed(() => {
  const groups = new Map<string, SearchResultItem[]>()
  for (const item of results.value) {
    if (!groups.has(item.category)) {
      groups.set(item.category, [])
    }
    groups.get(item.category)!.push(item)
  }
  const result: ResultGroup[] = []
  for (const [category, items] of groups) {
    result.push({ category, items })
  }
  return result
})

// Local search index (built-in shell items)
const localSearchIndex: SearchResultItem[] = [
  { title: 'Dashboard', description: 'Main dashboard with widgets', url: '/', category: 'Navigation', icon: 'Grid', score: 90 },
  { title: 'Users', description: 'Manage system users', url: '/users', category: 'Navigation', icon: 'User', score: 90 },
  { title: 'Roles', description: 'Manage roles and permissions', url: '/roles', category: 'Navigation', icon: 'Wallet', score: 90 },
  { title: 'Plugins', description: 'Manage installed plugins', url: '/plugins', category: 'Navigation', icon: 'Connection', score: 90 },
  { title: 'Settings', description: 'System configuration settings', url: '/settings', category: 'Navigation', icon: 'Setting', score: 90 },
  { title: 'Audit Log', description: 'View system audit trail', url: '/audit', category: 'Navigation', icon: 'Bell', score: 90 },
]

function onSearch(value: string) {
  if (debounceTimer) clearTimeout(debounceTimer)

  if (!value || value.length < 2) {
    results.value = []
    showResults.value = false
    return
  }

  showResults.value = true
  debounceTimer = setTimeout(() => performSearch(value), 150)
}

async function performSearch(value: string) {
  // Cancel previous request
  if (searchAbortController) {
    searchAbortController.abort()
  }
  searchAbortController = new AbortController()

  loading.value = true

  try {
    const q = value.toLowerCase()
    const combined: SearchResultItem[] = []

    // Local search
    for (const item of localSearchIndex) {
      if (item.title.toLowerCase().includes(q) ||
          (item.description && item.description.toLowerCase().includes(q))) {
        combined.push(item)
      }
    }

    // Search via plugins (if endpoint exists)
    try {
      const res = await authApi.get(`/api/extensions/search?q=${encodeURIComponent(value)}&limit=10`)
      if (res && res.data) {
        combined.push(...res.data)
      }
    } catch {
      // Plugin search endpoint may not exist yet - safe to ignore
    }

    // Deduplicate by URL
    const seen = new Set<string>()
    results.value = combined.filter(item => {
      if (seen.has(item.url)) return false
      seen.add(item.url)
      return true
    }).sort((a, b) => b.score - a.score)
  } catch (e: any) {
    if (e?.name !== 'AbortError') {
      console.warn('Search failed:', e)
    }
  } finally {
    loading.value = false
  }
}

function navigateTo(item: SearchResultItem) {
  showResults.value = false
  query.value = ''
  router.push(item.url)
}

function onFocus() {
  if (query.value.length >= 2) {
    showResults.value = true
  }
}

function onClose() {
  showResults.value = false
}

function getIconComponent(_icon: string): Component {
  void _icon
  return { template: `<span></span>` } as Component
}

// Keyboard shortcuts
function onKeyDown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
    e.preventDefault()
    searchRef.value?.querySelector('input')?.focus()
  }
  if (!showResults.value || results.value.length === 0) return

  if (e.key === 'ArrowDown') {
    e.preventDefault()
    hoveredIndex.value = Math.min(hoveredIndex.value + 1, results.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    hoveredIndex.value = Math.max(hoveredIndex.value - 1, 0)
  } else if (e.key === 'Enter' && hoveredIndex.value >= 0) {
    e.preventDefault()
    navigateTo(results.value[hoveredIndex.value])
  }
}

// Click outside handler
function onClickOutside(e: MouseEvent) {
  if (searchRef.value && !searchRef.value.contains(e.target as Node)) {
    showResults.value = false
  }
}

onMounted(() => {
  document.addEventListener('keydown', onKeyDown)
  document.addEventListener('click', onClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('keydown', onKeyDown)
  document.removeEventListener('click', onClickOutside)
  if (debounceTimer) clearTimeout(debounceTimer)
})
</script>

<style scoped>
.global-search {
  position: relative;
  width: 320px;
  min-width: 0;
}

.global-search__input {
  --el-input-bg-color: var(--el-fill-color-light);
  --el-input-border-color: transparent;
}

@media (max-width: 768px) {
  .global-search {
    width: 100%;
  }
}

.global-search__input:focus-within {
  --el-input-bg-color: var(--el-bg-color);
  --el-input-border-color: var(--el-color-primary);
}

.global-search__icon {
  color: var(--el-text-color-secondary);
}

.global-search__dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  box-shadow: var(--el-box-shadow-light);
  z-index: 2000;
  max-height: 480px;
  overflow-y: auto;
}

.global-search__loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 24px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.global-search__empty {
  padding: 16px;
}

.global-search__group {
  padding: 4px 0;
}

.global-search__group-label {
  padding: 6px 12px 2px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  color: var(--el-text-color-placeholder);
  letter-spacing: 0.5px;
}

.global-search__item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  cursor: pointer;
  transition: background 0.15s;
}

.global-search__item:hover {
  background: var(--el-fill-color-light);
}

.global-search__item-icon {
  flex-shrink: 0;
  width: 20px;
  color: var(--el-text-color-secondary);
}

.global-search__item-content {
  flex: 1;
  min-width: 0;
}

.global-search__item-title {
  font-size: 13px;
  color: var(--el-text-color-primary);
  font-weight: 500;
}

.global-search__item-desc {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.global-search__item-provider {
  font-size: 10px;
  color: var(--el-text-color-placeholder);
}

.global-search__footer {
  padding: 6px 12px;
  border-top: 1px solid var(--el-border-color-light);
}

.global-search__footer-hint {
  font-size: 11px;
  color: var(--el-text-color-placeholder);
}

.global-search__footer-hint kbd {
  display: inline-block;
  padding: 1px 4px;
  font-size: 11px;
  font-family: inherit;
  background: var(--el-fill-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 3px;
  margin: 0 1px;
}

.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>