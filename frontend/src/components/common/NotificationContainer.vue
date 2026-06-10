<template>
  <div v-if="notifications.length > 0" class="notification-container">
    <div
      v-for="notification in notifications"
      :key="notification.id"
      :class="['notification', `notification--${notification.type}`]"
      @click="notificationStore.removeNotification(notification.id)"
    >
      <span class="notification__icon">
        {{ iconFor(notification.type) }}
      </span>
      <span class="notification__message">{{ notification.message }}</span>
      <button
        class="notification__close"
        @click.stop="notificationStore.removeNotification(notification.id)"
      >
        ×
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useNotificationStore } from '@/stores/notificationStore'
import { storeToRefs } from 'pinia'

const notificationStore = useNotificationStore()
const { notifications } = storeToRefs(notificationStore)

function iconFor(type: string): string {
  switch (type) {
    case 'success': return '✓'
    case 'error': return '✕'
    case 'info': return 'ℹ'
    case 'warning': return '⚠'
    default: return 'ℹ'
  }
}
</script>