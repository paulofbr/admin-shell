<template>
  <div class="dashboard">
    <div class="dashboard__header">
      <div>
        <h2 class="dashboard__title">Dashboard</h2>
        <p class="dashboard__subtitle">Overview and key application metrics</p>
      </div>
      <div class="dashboard__header-actions">
        <el-button size="small" @click="refreshMetrics" :loading="loading">
          <el-icon><Refresh /></el-icon> Refresh
        </el-button>
        <AiAssistantButton
          context-title="Dashboard"
          context-subtitle="Overview and key application metrics"
          :context-meta="{ kind: 'dashboard' }"
        />
      </div>
    </div>

    <!-- Health Status -->
    <div class="dashboard__health">
      <div class="health-card" v-for="h in healthItems" :key="h.label" :class="'health-card--' + h.status">
        <div class="health-card__dot" :class="'health-card__dot--' + h.status"></div>
        <div class="health-card__info">
          <div class="health-card__label">{{ h.label }}</div>
          <div class="health-card__value">{{ h.value }}</div>
        </div>
        <el-tag :type="h.status === 'ok' ? 'success' : h.status === 'warn' ? 'warning' : 'danger'" size="small" effect="plain">
          {{ h.status === 'ok' ? 'Operational' : h.status === 'warn' ? 'Warning' : 'Error' }}
        </el-tag>
      </div>
    </div>

    <!-- Metrics Cards -->
    <el-row :gutter="16" class="dashboard__metrics">
      <el-col
        v-for="metric in metrics"
        :key="metric.key"
        :xs="24" :sm="12" :md="6" :lg="6"
        class="dashboard__metric-col"
      >
        <el-card shadow="hover" class="metric-card" :class="`metric-card--${metric.color}`" @click="metric.action">
          <div class="metric-card__icon">
            <el-icon :size="24"><component :is="metric.icon" /></el-icon>
          </div>
          <div class="metric-card__info">
            <span class="metric-card__value" v-loading="loading">
              {{ loading ? '' : metric.value }}
            </span>
            <span class="metric-card__label">{{ metric.label }}</span>
          </div>
          <div v-if="metric.change !== undefined" class="metric-card__change" :class="metric.change >= 0 ? 'up' : 'down'">
            <el-icon :size="12"><ArrowUp v-if="metric.change >= 0" /><ArrowDown v-else /></el-icon>
            {{ Math.abs(metric.change) }}{{ metric.isPercent ? '%' : '' }}
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- Second Row: Growth Chart + Recent Activity -->
    <el-row :gutter="16" class="dashboard__charts">
      <!-- User Growth Chart -->
      <el-col :xs="24" :md="16">
        <el-card shadow="never" class="chart-card">
          <template #header>
            <div class="chart-card__header">
              <span>User Growth (Monthly)</span>
              <div class="chart-card__header-tabs">
                <el-tag size="small" :type="period === 6 ? 'primary' : 'info'" style="cursor:pointer" @click="period = 6">6mo</el-tag>
                <el-tag size="small" :type="period === 12 ? 'primary' : 'info'" style="cursor:pointer" @click="period = 12">12mo</el-tag>
              </div>
            </div>
          </template>
          <div class="chart-card__body" v-loading="loading">
            <div v-if="userGrowth.length === 0 && !loading" class="chart-empty">
              <el-empty description="No growth data yet" />
            </div>
            <div v-else class="growth-chart">
              <div
                v-for="(point, i) in userGrowth"
                :key="i"
                class="growth-chart__bar-wrapper"
              >
                <div class="growth-chart__bar-label">{{ point.month }}</div>
                <div class="growth-chart__bar-track">
                  <div
                    class="growth-chart__bar"
                    :style="{ height: barHeight(point.count) + '%', background: barColor(i) }"
                    :title="`${point.month}: ${point.count} users`"
                  />
                </div>
                <div class="growth-chart__bar-value">{{ point.count }}</div>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- Recent Activity -->
      <el-col :xs="24" :md="8">
        <el-card shadow="never" class="chart-card">
          <template #header>
            <div class="chart-card__header">
              <span>Recent Activity</span>
              <el-tag size="small" type="warning">{{ auditToday }} today</el-tag>
            </div>
          </template>
          <div class="chart-card__body" v-loading="loading">
            <div class="activity-list">
              <div class="activity-item" v-for="act in recentActivity" :key="act.id">
                <div class="activity-item__dot" :class="`activity-item__dot--${act.type}`"></div>
                <div class="activity-item__content">
                  <div class="activity-item__text">{{ act.text }}</div>
                  <div class="activity-item__time">{{ act.time }}</div>
                </div>
              </div>
              <div v-if="recentActivity.length === 0 && !loading" class="chart-empty">
                <span style="color:var(--el-text-color-placeholder);font-size:13px">No recent activity</span>
              </div>
            </div>
            <el-divider v-if="auditByAction.length > 0" />
            <div v-if="auditByAction.length > 0" class="audit-breakdown">
              <div class="audit-breakdown__title">Breakdown (30 days)</div>
              <div v-for="item in auditByAction" :key="item.action" class="audit-breakdown__item">
                <span class="audit-breakdown__action">{{ item.action }}</span>
                <el-progress
                  :percentage="actionPercentage(item.count)"
                  :stroke-width="8"
                  :show-text="false"
                  :color="actionColor(item.action)"
                />
                <span class="audit-breakdown__count">{{ item.count }}</span>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- Third Row: Plugin Widgets + Quick Actions -->
    <el-row :gutter="16" class="dashboard__widgets">
      <el-col :xs="24" :md="12">
        <el-card shadow="never">
          <template #header>
            <div class="chart-card__header">
              <span>Plugin Widgets</span>
              <el-tag size="small" type="success">{{ pluginWidgets.length }} active</el-tag>
            </div>
          </template>
          <PluginSlot zone="dashboard" />
        </el-card>
      </el-col>

      <el-col :xs="24" :md="12">
        <el-card shadow="never">
          <template #header>
            <div class="chart-card__header">
              <span>Quick Actions</span>
            </div>
          </template>
          <div class="quick-actions">
            <div class="quick-action-card" @click="router.push('/users')">
              <div class="quick-action-card__icon" style="background:#eef2ff;color:#6366f1">👥</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Manage Users</div>
                <div class="quick-action-card__desc">Create, edit, deactivate</div>
              </div>
            </div>
            <div class="quick-action-card" @click="router.push('/roles')">
              <div class="quick-action-card__icon" style="background:#ecfdf5;color:#10b981">🔐</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Manage Roles</div>
                <div class="quick-action-card__desc">Permissions & access</div>
              </div>
            </div>
            <div class="quick-action-card" @click="router.push('/audit')">
              <div class="quick-action-card__icon" style="background:#fffbeb;color:#f59e0b">📋</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Audit Log</div>
                <div class="quick-action-card__desc">Track all changes</div>
              </div>
            </div>
            <div class="quick-action-card" @click="router.push('/plugins')">
              <div class="quick-action-card__icon" style="background:#f5f3ff;color:#8b5cf6">🧩</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Plugins</div>
                <div class="quick-action-card__desc">Manage extensions</div>
              </div>
            </div>
            <div class="quick-action-card" @click="router.push('/settings')">
              <div class="quick-action-card__icon" style="background:#fef2f2;color:#ef4444">⚙️</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Settings</div>
                <div class="quick-action-card__desc">Configure system</div>
              </div>
            </div>
            <div class="quick-action-card" @click="router.push('/logs')">
              <div class="quick-action-card__icon" style="background:#f0f9ff;color:#0ea5e9">📄</div>
              <div class="quick-action-card__info">
                <div class="quick-action-card__title">Logs</div>
                <div class="quick-action-card__desc">View system logs</div>
              </div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useExtensionStore } from '@/stores/extensionStore'
import { getDashboardMetrics } from '@/api/dashboard'
import PluginSlot from '@/components/plugins/PluginSlot.vue'
import AiAssistantButton from '@admin-shell/ui/AiAssistantButton.vue'
import {
  User,
  Wallet,
  Connection,
  Bell,
  Setting,
  Refresh,
  ArrowUp,
  ArrowDown,
  CircleCheckFilled,
  CircleCloseFilled,
  WarningFilled,
} from '@element-plus/icons-vue'

const router = useRouter()
const extensionStore = useExtensionStore()

const loading = ref(true)
const period = ref(6)
const metricsData = ref({
  users: { total: 0, active: 0, inactive: 0, monthlyGrowth: [] as { month: string; count: number }[] },
  roles: { total: 0 },
  plugins: { total: 0, active: 0 },
  audit: { today: 0, loginsToday: 0, failedLoginsToday: 0, byAction: [] as { action: string; count: number }[] },
})

const userGrowth = computed(() => metricsData.value.users.monthlyGrowth)
const auditToday = computed(() => metricsData.value.audit.today)
const loginsToday = computed(() => metricsData.value.audit.loginsToday)
const failedLoginsToday = computed(() => metricsData.value.audit.failedLoginsToday)
const auditByAction = computed(() => metricsData.value.audit.byAction)

const metrics = computed(() => [
  { key: 'users', label: 'Total Users', value: metricsData.value.users.total, icon: User, color: 'primary',
    change: metricsData.value.users.monthlyGrowth.length >= 2
      ? Math.round(((metricsData.value.users.monthlyGrowth[metricsData.value.users.monthlyGrowth.length - 1]?.count ?? 0) -
          (metricsData.value.users.monthlyGrowth[metricsData.value.users.monthlyGrowth.length - 2]?.count ?? 0)) / Math.max(1, (metricsData.value.users.monthlyGrowth[metricsData.value.users.monthlyGrowth.length - 2]?.count ?? 1)) * 100)
      : undefined,
    isPercent: true,
    action: () => router.push('/users') },
  { key: 'active', label: 'Active Users', value: metricsData.value.users.active, icon: User, color: 'success',
    change: metricsData.value.users.total > 0
      ? Math.round(metricsData.value.users.active / metricsData.value.users.total * 100)
      : 0,
    isPercent: true,
    action: () => router.push('/users') },
  { key: 'roles', label: 'Roles', value: metricsData.value.roles.total, icon: Wallet, color: 'warning',
    change: undefined, isPercent: false,
    action: () => router.push('/roles') },
  { key: 'plugins', label: 'Plugins Active', value: `${metricsData.value.plugins.active}/${metricsData.value.plugins.total}`, icon: Connection, color: 'info',
    change: metricsData.value.plugins.active, isPercent: false,
    action: () => router.push('/plugins') },
])

const healthItems = computed(() => [
  { label: 'API Server', value: 'Connected', status: 'ok' as const },
  { label: 'Database', value: 'Connected', status: 'ok' as const },
  { label: 'Plugins', value: `${metricsData.value.plugins.active}/${metricsData.value.plugins.total} active`, status: metricsData.value.plugins.active > 0 ? 'ok' as const : 'warn' as const },
  { label: 'Cache', value: 'Operational', status: 'ok' as const },
])

const recentActivity = computed(() => {
  const items: { id: number; type: string; text: string; time: string }[] = []
  if (metricsData.value.audit.loginsToday > 0) {
    items.push({ id: 1, type: 'success', text: `${metricsData.value.audit.loginsToday} successful login${metricsData.value.audit.loginsToday > 1 ? 's' : ''}`, time: 'Today' })
  }
  if (metricsData.value.audit.failedLoginsToday > 0) {
    items.push({ id: 2, type: 'danger', text: `${metricsData.value.audit.failedLoginsToday} failed login attempt${metricsData.value.audit.failedLoginsToday > 1 ? 's' : ''}`, time: 'Today' })
  }
  if (metricsData.value.audit.today > 0) {
    items.push({ id: 3, type: 'warning', text: `${metricsData.value.audit.today} total events today`, time: 'Today' })
  }
  auditByAction.value.forEach((a, i) => {
    items.push({ id: 4 + i, type: 'info', text: `${a.action}: ${a.count} events`, time: '30 days' })
  })
  return items.slice(0, 8)
})

const maxGrowth = computed(() => {
  if (userGrowth.value.length === 0) return 1
  return Math.max(...userGrowth.value.map(p => p.count), 1)
})

function barHeight(count: number): number {
  return Math.max(5, (count / maxGrowth.value) * 100)
}

function barColor(index: number): string {
  const colors = [
    'linear-gradient(to top, #6366f1, #818cf8)',
    'linear-gradient(to top, #6366f1, #a5b4fc)',
    'linear-gradient(to top, #8b5cf6, #a78bfa)',
  ]
  return colors[index % colors.length]
}

const maxAuditCount = computed(() => {
  if (auditByAction.value.length === 0) return 1
  return Math.max(...auditByAction.value.map(a => a.count), 1)
})

function actionPercentage(count: number): number {
  return (count / maxAuditCount.value) * 100
}

function actionColor(action: string): string {
  const colors: Record<string, string> = {
    LOGIN: '#67c23a',
    LOGIN_FAILED: '#f56c6c',
    LOGOUT: '#909399',
    USER_CREATE: '#409eff',
    USER_UPDATE: '#e6a23c',
    USER_DELETE: '#f56c6c',
  }
  return colors[action] ?? '#909399'
}

const pluginWidgets = computed(() => extensionStore.getWidgetsByZone('dashboard'))

async function loadMetrics() {
  loading.value = true
  try {
    const metrics = await getDashboardMetrics()
    metricsData.value = {
      users: {
        total: Number(metrics.users.total),
        active: Number(metrics.users.active),
        inactive: Number(metrics.users.inactive),
        monthlyGrowth: metrics.users.monthlyGrowth.map((point) => ({
          month: point.month,
          count: Number(point.count),
        })),
      },
      roles: {
        total: Number(metrics.roles.total),
      },
      plugins: {
        total: Number(metrics.plugins.total),
        active: Number(metrics.plugins.active),
      },
      audit: {
        today: Number(metrics.audit.today),
        loginsToday: Number(metrics.audit.loginsToday),
        failedLoginsToday: Number(metrics.audit.failedLoginsToday),
        byAction: metrics.audit.byAction.map((item) => ({
          action: item.action,
          count: Number(item.count),
        })),
      },
    }
  } catch (e: unknown) {
    console.warn('Failed to load metrics:', e)
  } finally {
    loading.value = false
  }
}

function refreshMetrics() {
  loadMetrics()
}

onMounted(() => {
  loadMetrics()
})
</script>

<style scoped>
.dashboard {
  padding: 24px;
  max-width: 100%;
  overflow-x: hidden;
}

.dashboard__header-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

/* Health cards */
.dashboard__health {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 12px;
  margin-bottom: 20px;
}

.health-card {
  display: flex;
  align-items: center;
  gap: 12px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 10px;
  padding: 14px 18px;
  transition: box-shadow 0.2s;
}

.health-card:hover {
  box-shadow: var(--el-box-shadow-light);
}

.health-card--ok { border-left: 3px solid var(--el-color-success); }
.health-card--warn { border-left: 3px solid var(--el-color-warning); }
.health-card--error { border-left: 3px solid var(--el-color-danger); }

.health-card__dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  flex-shrink: 0;
}

.health-card__dot--ok { background: var(--el-color-success); box-shadow: 0 0 6px var(--el-color-success); }
.health-card__dot--warn { background: var(--el-color-warning); box-shadow: 0 0 6px var(--el-color-warning); }
.health-card__dot--error { background: var(--el-color-danger); box-shadow: 0 0 6px var(--el-color-danger); }

.health-card__info {
  flex: 1;
  min-width: 0;
}

.health-card__label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  font-weight: 500;
}

.health-card__value {
  font-size: 14px;
  font-weight: 600;
  margin-top: 1px;
}

/* Activity list */
.activity-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.activity-item {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 8px 0;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.activity-item:last-child { border-bottom: none; }

.activity-item__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-top: 5px;
  flex-shrink: 0;
}

.activity-item__dot--success { background: var(--el-color-success); }
.activity-item__dot--danger { background: var(--el-color-danger); }
.activity-item__dot--warning { background: var(--el-color-warning); }
.activity-item__dot--info { background: var(--el-color-primary); }

.activity-item__content { flex: 1; min-width: 0; }

.activity-item__text {
  font-size: 13px;
  font-weight: 500;
  line-height: 1.4;
}

.activity-item__time {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-top: 1px;
}

.chart-card__header-tabs {
  display: flex;
  gap: 4px;
}

.dashboard__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 20px;
}

.dashboard__title {
  margin: 0;
  font-size: 20px;
}

.dashboard__subtitle {
  margin: 4px 0 0;
  color: var(--el-text-color-secondary);
  font-size: 14px;
}

.dashboard__metrics {
  margin-bottom: 16px;
}

.dashboard__metric-col {
  margin-bottom: 16px;
}

.metric-card {
  cursor: pointer;
  transition: transform 0.15s, box-shadow 0.15s;
  position: relative;
  overflow: hidden;
}

.metric-card:hover {
  transform: translateY(-2px);
}

.metric-card--primary { --metric-accent: var(--el-color-primary); }
.metric-card--success { --metric-accent: var(--el-color-success); }
.metric-card--warning { --metric-accent: var(--el-color-warning); }
.metric-card--info { --metric-accent: var(--el-color-info); }
.metric-card--danger { --metric-accent: var(--el-color-danger); }

.metric-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  width: 4px;
  height: 100%;
  background: var(--metric-accent);
}

.metric-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
}

.metric-card__icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  background: color-mix(in srgb, var(--metric-accent) 12%, transparent);
  color: var(--metric-accent);
  flex-shrink: 0;
}

.metric-card__info {
  display: flex;
  flex-direction: column;
  flex: 1 1 auto;
  min-width: 0;
}

.metric-card__value {
  font-size: 24px;
  font-weight: 700;
  line-height: 1.2;
  min-height: 30px;
  min-width: 0;
  overflow-wrap: anywhere;
}

.metric-card__label {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.metric-card__change {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: 12px;
  font-weight: 500;
  padding: 2px 8px;
  border-radius: 12px;
  align-self: flex-start;
}

.metric-card__change.up {
  color: var(--el-color-success);
  background: color-mix(in srgb, var(--el-color-success) 10%, transparent);
}

.metric-card__change.down {
  color: var(--el-color-danger);
  background: color-mix(in srgb, var(--el-color-danger) 10%, transparent);
}

/* Chart card */
.chart-card :deep(.el-card__body) {
  padding: 16px;
}

.chart-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  min-width: 0;
  overflow: hidden;
}

.chart-card__body {
  min-height: 160px;
}

.chart-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 160px;
  color: var(--el-text-color-placeholder);
}

/* Bar chart */
.growth-chart {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  height: 180px;
  padding: 0 4px;
  min-width: 0;
  overflow-x: hidden;
}

.growth-chart__bar-wrapper {
  flex: 1 1 0;
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  height: 100%;
  justify-content: flex-end;
}

.growth-chart__bar-label {
  font-size: 10px;
  color: var(--el-text-color-secondary);
  margin-top: 6px;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.growth-chart__bar-track {
  width: 100%;
  min-width: 0;
  height: 160px;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.growth-chart__bar {
  width: 70%;
  max-width: 40px;
  min-width: 4px;
  background: linear-gradient(to top, var(--el-color-primary), var(--el-color-primary-light-3));
  border-radius: 4px 4px 0 0;
  transition: height 0.5s ease;
  min-height: 4px;
}

.growth-chart__bar-value {
  font-size: 10px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
  margin-top: 2px;
}

.audit-breakdown__title {
  font-size: 12px;
  font-weight: 600;
  color: var(--el-text-color-secondary);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.audit-breakdown__item {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
  min-width: 0;
}

.audit-breakdown__action {
  font-size: 11px;
  width: 100px;
  max-width: 42%;
  flex-shrink: 0;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.audit-breakdown__item :deep(.el-progress) {
  flex: 1 1 auto;
  min-width: 0;
}

.audit-breakdown__count {
  font-size: 12px;
  font-weight: 600;
  width: 30px;
  max-width: 18%;
  text-align: right;
  flex-shrink: 0;
}

/* Widgets */
.widget-placeholder {
  padding: 20px 0;
}

.widget-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 8px;
  min-width: 0;
}

.widget-item {
  padding: 12px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  transition: border-color 0.15s;
  min-width: 0;
  overflow-wrap: anywhere;
}

.widget-item:hover {
  border-color: var(--el-color-primary);
}

.widget-item__title {
  font-size: 13px;
  font-weight: 500;
  margin-bottom: 6px;
}

.widget-item__meta {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
  flex-wrap: wrap;
}

.widget-item__size {
  font-size: 11px;
  color: var(--el-text-color-secondary);
}

/* Quick actions */
.quick-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  width: 100%;
  min-width: 0;
}

.quick-action-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s;
  background: var(--el-bg-color);
}

.quick-action-card:hover {
  border-color: var(--el-color-primary);
  background: var(--el-color-primary-light-9);
  transform: translateY(-1px);
}

.quick-action-card__icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}

.quick-action-card__info {
  flex: 1;
  min-width: 0;
}

.quick-action-card__title {
  font-size: 13px;
  font-weight: 600;
}

.quick-action-card__desc {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  margin-top: 1px;
}

.dashboard__widgets {
  margin-top: 16px;
}

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .dashboard {
    padding: 0;
    overflow-x: hidden;
  }

.dashboard__header {
  flex-direction: column;
  align-items: flex-start;
}

.dashboard__title {
  font-size: 20px;
  margin-bottom: 4px;
}

  .dashboard__metrics,
  .dashboard__charts,
  .dashboard__widgets {
    width: 100%;
    max-width: 100%;
  }

  .dashboard__metric-col {
    margin-bottom: 12px;
    width: 100%;
    max-width: 100%;
  }

  .metric-card,
  .chart-card {
    width: 100%;
    max-width: 100%;
  }

  .metric-card :deep(.el-card__body) {
    padding: 16px;
    gap: 12px;
  }

  .metric-card__value {
    font-size: 22px;
  }

  .chart-card__header {
    align-items: flex-start;
  }

  .chart-card__header > span {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .chart-card__header :deep(.el-tag) {
    flex-shrink: 0;
    max-width: 45%;
    overflow: hidden;
  }

  .chart-card__header :deep(.el-tag__content) {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .growth-chart {
    height: 120px;
    padding: 0 2px;
    overflow-x: hidden;
  }

  .growth-chart__bar-track {
    height: 100px;
  }

  .growth-chart__bar {
    width: 60%;
    max-width: 24px;
  }

  .audit-breakdown__action {
    width: 86px;
    max-width: 34%;
  }

  .audit-breakdown__count {
    width: 24px;
    max-width: 14%;
  }

  .quick-actions {
    width: 100%;
    grid-template-columns: 1fr;
  }

  .dashboard__health {
    grid-template-columns: 1fr 1fr;
  }

  .widget-grid {
    grid-template-columns: 1fr;
  }

  /* Grid adjustments for mobile - prevent overflow */
  :deep(.el-row) {
    margin-left: 0 !important;
    margin-right: 0 !important;
    width: 100%;
    max-width: 100%;
  }

  :deep(.el-row.dashboard__metrics) {
    margin-left: 0 !important;
    margin-right: 0 !important;
  }

  :deep(.el-col) {
    padding-left: 0 !important;
    padding-right: 8px !important;
    max-width: 100%;
  }

  :deep(.el-row.dashboard__metrics) .dashboard__metric-col {
    flex-basis: 100% !important;
    width: 100% !important;
    max-width: 100% !important;
    min-width: 100% !important;
    padding-left: 0 !important;
    padding-right: 0 !important;
  }
}
</style>