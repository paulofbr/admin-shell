import { defineStore } from 'pinia'
import type { NotificationItem } from '../types'

interface NotificationState {
  notifications: NotificationItem[]
}

let notificationIdCounter = 0

export const useNotificationStore = defineStore('notification', {
  state: (): NotificationState => ({
    notifications: [],
  }),
  getters: {
    unreadCount: (state) => state.notifications.length,
  },
  actions: {
    addNotification(message: string, type: NotificationItem['type']) {
      const id = `${Date.now()}-${++notificationIdCounter}`
      this.notifications.push({ id, message, type })

      setTimeout(() => {
        this.removeNotification(id)
      }, 3000)
    },

    removeNotification(id: string) {
      this.notifications = this.notifications.filter((n) => n.id !== id)
    },
  },
})
