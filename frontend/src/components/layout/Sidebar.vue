<template>
  <!-- Overlay for mobile -->
  <div v-if="isOpen" class="sidebar-overlay" @click="$emit('close')" />
  <aside :class="['sidebar', { 'sidebar--open': isOpen, 'sidebar--collapsed': isCollapsed }]" class="hidden md:block">
    <!-- Logo / Brand -->
    <div class="sidebar__brand">
      <div class="sidebar__logo">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <rect x="14" y="14" width="7" height="7" rx="1" />
        </svg>
      </div>
      <div v-if="!isCollapsed" class="sidebar__brand-text">
        <span class="sidebar__brand-title">{{ applicationName }}</span>
        <span class="sidebar__brand-subtitle">{{ applicationSubtitle }}</span>
      </div>
    </div>

    <!-- Navigation -->
    <el-menu
      :default-active="activeMenu"
      :collapse="isCollapsed"
      text-color="rgba(255,255,255,0.75)"
      active-text-color="#fff"
      background-color="transparent"
      unique-opened
      @select="handleSelect"
      class="sidebar__menu"
    >
      <!-- Main section -->
      <div class="sidebar__section-label">Main</div>
      <el-menu-item
        v-for="item in mainMenuItems"
        :key="item.id"
        :index="item.path"
      >
        <el-icon><component :is="item.icon" /></el-icon>
        <span>{{ item.label }}</span>
      </el-menu-item>

      <!-- Plugin sections (from ISidebarSectionPlugin) -->
      <template v-for="section in sidebarSections" :key="section.id">
        <div class="sidebar__section-label sidebar__section-label--plugins">
          <el-icon v-if="section.icon" style="margin-right: 4px; font-size: 12px;">
            <component :is="getIcon(section.icon)" />
          </el-icon>
          {{ section.label }}
        </div>
        <el-menu-item
          v-for="item in section.items"
          :key="item.id"
          :index="item.path"
        >
          <el-icon v-if="item.icon">
            <component :is="getIcon(item.icon)" />
          </el-icon>
          <span>{{ item.label }}</span>
        </el-menu-item>
      </template>

      <!-- Plugin menu items (from IMenuPlugin) -->
      <template v-if="topLevelMenuItems.length > 0">
        <div class="sidebar__section-label sidebar__section-label--plugins">Plugins</div>
        <template v-for="item in topLevelMenuItems" :key="item.id">
          <el-sub-menu v-if="item.children && item.children.length > 0" :index="item.path ?? '#'">
            <template #title>
              <el-icon v-if="item.icon">
                <component :is="getIcon(item.icon)" />
              </el-icon>
              <span>{{ item.label }}</span>
            </template>
            <el-menu-item
              v-for="child in item.children"
              :key="child.id"
              :index="child.path ?? '#'"
            >
              <el-icon v-if="child.icon">
                <component :is="getIcon(child.icon)" />
              </el-icon>
              <span>{{ child.label }}</span>
            </el-menu-item>
          </el-sub-menu>
          <el-menu-item v-else :index="item.path ?? '#'">
            <el-icon v-if="item.icon">
              <component :is="getIcon(item.icon)" />
            </el-icon>
            <span>{{ item.label }}</span>
          </el-menu-item>
        </template>
      </template>
    </el-menu>

    <!-- Bottom section -->
    <div class="sidebar__footer">
      <div class="sidebar__version">v1.0.0</div>
      <button 
        @click="$emit('toggleCollapse')"
        class="sidebar__collapse-btn"
        :title="isCollapsed ? 'Expand menu' : 'Collapse menu'"
      >
        <el-icon :size="16">
          <Fold v-if="!isCollapsed" />
          <Expand v-else />
        </el-icon>
      </button>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useApplicationStore } from '@/stores/applicationStore'
import { useExtensionStore } from '@/stores/extensionStore'
import type { Component } from 'vue'
import { storeToRefs } from 'pinia'
import { Grid, User, Connection, Setting, Wallet, ChatDotSquare, Document, Bell, DataAnalysis,
  List, DataBoard, UserFilled, OfficeBuilding, Fold, Expand } from '@element-plus/icons-vue'

defineProps<{
  isOpen: boolean
  isCollapsed: boolean
}>()

defineEmits<{
  close: []
  toggleCollapse: []
}>()

const route = useRoute()
const router = useRouter()
const applicationStore = useApplicationStore()
const extensionStore = useExtensionStore()
const { applicationName, applicationSubtitle } = storeToRefs(applicationStore)

const iconMap: Record<string, Component> = {
  grid: Grid,
  users: User,
  connection: Connection,
  settings: Setting,
  wallet: Wallet,
  chat: ChatDotSquare,
  document: Document,
  bell: Bell,
  analysis: DataAnalysis,
  list: List,
  databoard: DataBoard,
  userfilled: UserFilled,
  officebuilding: OfficeBuilding,
}

const mainMenuItems = [
  { id: 'dashboard', label: 'Dashboard', icon: Grid, path: '/' },
  { id: 'users', label: 'Users', icon: User, path: '/users' },
  { id: 'roles', label: 'Roles', icon: Wallet, path: '/roles' },
  { id: 'plugins', label: 'Plugins', icon: Connection, path: '/plugins' },
  { id: 'settings', label: 'Settings', icon: Setting, path: '/settings' },
  { id: 'logs', label: 'Logs', icon: Document, path: '/logs' },
]

const sidebarSections = computed(() => extensionStore.getSidebarSectionsSorted)
const allMenuItems = computed(() => extensionStore.menuItemsSorted)

// Top-level menu items (those without ParentId)
const topLevelMenuItems = computed(() =>
  allMenuItems.value.filter(item => !item.parentId)
)

const activeMenu = computed(() => {
  if (route.path === '/') return '/'
  return route.path
})

function handleSelect(index: string) {
  router.push(index)
}

function getIcon(icon: string): Component {
  const key = icon.toLowerCase().replace(/[^a-z0-9]/g, '')
  return iconMap[key] ?? Document
}
</script>

<style scoped>
.sidebar {
  position: fixed;
  top: 0;
  left: 0;
  width: 240px;
  height: 100vh;
  background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);
  display: flex;
  flex-direction: column;
  transition: width 0.25s ease;
  z-index: 1000;
}

.sidebar--collapsed {
  width: 64px;
}

.sidebar--open {
  transform: translateX(0);
}

.sidebar-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.4);
  z-index: 999;
}

.sidebar__brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 16px;
  height: 56px;
  flex-shrink: 0;
}

.sidebar__logo {
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--el-color-primary);
}

.sidebar__logo svg {
  width: 24px;
  height: 24px;
}

.sidebar__brand-text {
  display: flex;
  flex-direction: column;
}

.sidebar__brand-title {
  font-size: 14px;
  font-weight: 600;
  color: #fff;
  line-height: 1.2;
}

.sidebar__brand-subtitle {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  line-height: 1.2;
}

.sidebar__menu {
  flex: 1;
  overflow-y: auto;
  border: none;
}

.sidebar__menu :deep(.el-menu-item),
.sidebar__menu :deep(.el-sub-menu__title) {
  height: 40px;
  line-height: 40px;
  border-radius: 0 20px 20px 0;
  margin-right: 8px;
  transition: background 0.15s;
}

.sidebar__menu :deep(.el-menu-item.is-active) {
  background: var(--el-color-primary);
  color: #fff !important;
}

.sidebar__section-label {
  font-size: 11px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  padding: 8px 16px;
  margin-top: 4px;
}

.sidebar__section-label--plugins {
  display: flex;
  align-items: center;
}

.sidebar__footer {
  padding: 12px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.08);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.sidebar__version {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.sidebar__collapse-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  background: rgba(255, 255, 255, 0.08);
  color: var(--el-text-color-secondary);
  cursor: pointer;
  border-radius: 6px;
  transition: background 0.15s;
}

.sidebar__collapse-btn:hover {
  background: rgba(255, 255, 255, 0.15);
  color: #fff;
}
</style>