import { createRouter, createWebHistory } from 'vue-router'
import eventBus from '@/utils/eventBus'
import { usePluginStore } from '@/stores/pluginStore'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/pages/LoginPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/register',
      name: 'Register',
      component: () => import('@/pages/RegisterPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/',
      component: () => import('@/components/layout/Layout.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          name: 'Dashboard',
          component: () => import('@/pages/DashboardPage.vue'),
        },
        {
          path: 'users',
          name: 'Users',
          component: () => import('@/pages/UsersPage.vue'),
        },
        {
          path: 'roles',
          name: 'Roles',
          component: () => import('@/pages/RolesPage.vue'),
        },
        {
          path: 'plugins',
          name: 'Plugins',
          component: () => import('@/pages/PluginsPage.vue'),
        },
        {
          path: 'settings',
          name: 'Settings',
          component: () => import('@/pages/SettingsPage.vue'),
        },
        {
          path: 'audit',
          name: 'AuditLog',
          component: () => import('@/pages/AuditLogPage.vue'),
        },
        {
          path: 'profile',
          name: 'Profile',
          component: () => import('@/pages/ProfilePage.vue'),
        },
        {
          path: 'orders',
          name: 'Orders',
          component: () => import('@/pages/OrdersPage.vue'),
          meta: { requiresAuth: true, pluginId: 'order-creation' },
        },
        {
          path: 'orders/create',
          name: 'OrderCreate',
          component: () => import('@/pages/OrderCreatePage.vue'),
          meta: { requiresAuth: true, pluginId: 'order-creation' },
        },
        {
          path: 'orders/summary',
          name: 'OrderSummary',
          component: () => import('@/pages/OrderSummaryPage.vue'),
          meta: { requiresAuth: true, pluginId: 'order-creation' },
        },
        {
          path: 'vscode-web',
          name: 'VsCodeWeb',
          component: () => import('@/pages/VsCodeWebPage.vue'),
        },
      ],
    },
  ],
})

router.beforeEach(async (to, _from, next) => {
  // Check localStorage directly - more reliable than Pinia store during initialization
  const hasToken = !!localStorage.getItem('auth_token')
  const hasRefresh = !!localStorage.getItem('auth_refresh')
  const isAuthenticated = hasToken && hasRefresh

  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.meta.guest && isAuthenticated) {
    next('/')
  } else if (typeof to.meta.pluginId === 'string') {
    const pluginStore = usePluginStore()
    await pluginStore.loadPluginManifests().catch((error) => {
      console.warn('Failed to refresh plugin discovery before route guard:', error)
    })

    if (!pluginStore.isPluginActive(to.meta.pluginId)) {
      next('/')
    } else {
      next()
    }
  } else {
    next()
  }
})

router.afterEach((to) => {
  eventBus.publish('route:changed', to.path)
})

export default router