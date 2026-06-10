import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

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
        // Catch plugin section routes to auto-generate pages
        {
          path: 'departments',
          name: 'DepartmentOverview',
          component: () => import('@/pages/plugins/DepartmentOverviewPage.vue'),
        },
        {
          path: 'departments/members',
          name: 'DepartmentMembers',
          component: () => import('@/pages/plugins/DepartmentMembersPage.vue'),
        },
        {
          path: 'departments/report',
          name: 'DepartmentReport',
          component: () => import('@/pages/plugins/DepartmentReportPage.vue'),
        },
        // Catch-all for plugin routes
        {
          path: 'reports',
          name: 'Reports',
          component: () => import('@/pages/plugins/ReportsPage.vue'),
        },
        {
          path: 'reports/dashboard',
          name: 'ReportsDashboard',
          component: () => import('@/pages/plugins/ReportsDashboardPage.vue'),
        },
        {
          path: 'reports/analytics',
          name: 'ReportsAnalytics',
          component: () => import('@/pages/plugins/ReportsAnalyticsPage.vue'),
        },
        {
          path: 'reports/create',
          name: 'ReportsCreate',
          component: () => import('@/pages/plugins/ReportsCreatePage.vue'),
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

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login')
  } else if (to.meta.guest && authStore.isAuthenticated) {
    next('/')
  } else {
    next()
  }
})

export default router