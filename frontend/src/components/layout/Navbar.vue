<template>
  <header class="navbar">
    <div class="navbar__left">
      <button class="navbar__menu-toggle" @click="$emit('menuToggle')">
        <el-icon :size="20"><Fold /></el-icon>
      </button>
      <el-breadcrumb separator="/" class="navbar__breadcrumb">
        <el-breadcrumb-item :to="{ path: '/' }">Admin Shell</el-breadcrumb-item>
        <el-breadcrumb-item v-if="route.path !== '/'">
          {{ currentPage }}
        </el-breadcrumb-item>
      </el-breadcrumb>
    </div>

    <!-- Global Search -->
    <div class="navbar__center">
      <GlobalSearchBar />
    </div>

    <div class="navbar__right">
      <!-- Plugin Header Actions -->
      <HeaderActions target="header" />

      <!-- Notification Bell -->
      <el-tooltip content="Notifications" placement="bottom">
        <el-badge :value="unreadNotifications" :hidden="unreadNotifications === 0" class="navbar__badge">
          <el-button circle size="small" class="navbar__icon-btn" @click="showNotifications = !showNotifications">
            <el-icon :size="18"><Bell /></el-icon>
          </el-button>
        </el-badge>
      </el-tooltip>

      <!-- Theme Toggle -->
      <el-tooltip content="Toggle theme" placement="bottom">
        <el-button circle size="small" class="navbar__icon-btn" @click="toggleTheme">
          <el-icon :size="18"><Moon /></el-icon>
        </el-button>
      </el-tooltip>

      <!-- User Dropdown -->
      <el-dropdown v-if="user" trigger="click" @command="handleCommand">
        <div class="navbar__user">
          <div class="navbar__avatar">
            <img
              v-if="user.avatarUrl"
              :src="user.avatarUrl"
              :alt="user.displayName ?? user.username"
              class="navbar__avatar-img"
            />
            <span v-else class="navbar__avatar-placeholder">
              {{ (user.displayName ?? user.username).charAt(0).toUpperCase() }}
            </span>
          </div>
          <div class="navbar__user-info">
            <span class="navbar__username">{{ user.displayName ?? user.username }}</span>
            <span class="navbar__role">{{ primaryRole }}</span>
          </div>
          <el-icon class="navbar__chevron"><ArrowDown /></el-icon>
        </div>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">
              <el-icon><User /></el-icon> Profile
            </el-dropdown-item>
            <el-dropdown-item command="settings">
              <el-icon><Setting /></el-icon> Settings
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <el-icon><SwitchButton /></el-icon> Logout
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useNotificationStore } from '@/stores/notificationStore'
import { storeToRefs } from 'pinia'
import {
  Fold,
  Bell,
  Moon,
  ArrowDown,
  User,
  Setting,
  SwitchButton,
} from '@element-plus/icons-vue'
import GlobalSearchBar from '@/components/common/GlobalSearchBar.vue'
import HeaderActions from '@/components/common/HeaderActions.vue'

defineEmits<{
  menuToggle: []
}>()

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const notificationStore = useNotificationStore()
const { user } = storeToRefs(authStore)
const { unreadCount: unreadNotifications } = storeToRefs(notificationStore)
const showNotifications = ref(false)

const currentPage = computed(() => {
  const name = route.name
  return typeof name === 'string' ? name : route.path.slice(1).replace(/^(\w)/, c => c.toUpperCase())
})

const primaryRole = computed(() => {
  if (!user.value || user.value.roles.length === 0) return ''
  return user.value.roles[0].name
})

async function handleCommand(command: string) {
  if (command === 'logout') {
    await authStore.logout()
    router.push('/login')
  } else if (command === 'settings') {
    router.push('/settings')
  } else if (command === 'profile') {
    router.push('/profile')
  }
}

function toggleTheme() {
  const current = document.documentElement.getAttribute('data-theme') ?? 'light'
  const next = current === 'light' ? 'dark' : 'light'
  document.documentElement.setAttribute('data-theme', next)
}</script>

<style scoped>
.navbar {
  display: flex;
  align-items: center;
  height: var(--navbar-height, 56px);
  padding: 0 16px;
  background: var(--el-bg-color);
  border-bottom: 1px solid var(--el-border-color-light);
  gap: 12px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar__left {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.navbar__menu-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  background: transparent;
  color: var(--el-text-color-primary);
  cursor: pointer;
  border-radius: 6px;
}

.navbar__menu-toggle:hover {
  background: var(--el-fill-color-light);
}

.navbar__breadcrumb {
  font-size: 13px;
}

.navbar__center {
  flex: 1;
  display: flex;
  justify-content: center;
}

.navbar__right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.navbar__icon-btn {
  --el-button-size: 32px;
  color: var(--el-text-color-secondary);
}

.navbar__icon-btn:hover {
  color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
}

.navbar__badge :deep(.el-badge__content) {
  border: 2px solid var(--el-bg-color);
}

.navbar__user {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background 0.15s;
}

.navbar__user:hover {
  background: var(--el-fill-color-light);
}

.navbar__avatar {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  overflow: hidden;
  flex-shrink: 0;
}

.navbar__avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.navbar__avatar-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  background: var(--el-color-primary-light-8);
  color: var(--el-color-primary);
  font-size: 12px;
  font-weight: 600;
}

.navbar__user-info {
  display: flex;
  flex-direction: column;
  line-height: 1.2;
}

.navbar__username {
  font-size: 13px;
  font-weight: 500;
  color: var(--el-text-color-primary);
}

.navbar__role {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

.navbar__chevron {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}
</style>