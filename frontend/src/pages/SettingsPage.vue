<template>
  <div class="page">
    <div class="page__header">
      <div>
        <h2 class="page__title">Settings</h2>
        <p class="page__subtitle">Configure application settings</p>
      </div>
      <el-button
        type="primary"
        :icon="Check"
        round
        :loading="settingsStore.saving"
        :disabled="!hasChanges"
        @click="saveSettings"
      >
        Save Changes
      </el-button>
    </div>

    <!-- Loading state -->
    <el-card v-if="settingsStore.loading" shadow="never">
      <el-skeleton :rows="6" animated />
    </el-card>

    <!-- Error state -->
    <el-alert
      v-else-if="settingsStore.error"
      :title="settingsStore.error"
      type="error"
      show-icon
      closable
    />

    <!-- Settings form -->
    <el-card v-else shadow="never">
      <el-tabs v-model="activeTab" class="settings-tabs">
        <el-tab-pane label="General" name="general">
          <el-form label-position="top" style="max-width: 500px">
            <el-form-item label="Site Name">
              <el-input v-model="form.siteName" placeholder="Enter site name" />
            </el-form-item>
            <el-form-item label="Site Description">
              <el-input v-model="form.siteDescription" type="textarea" :rows="3" placeholder="Enter site description" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.enableRegistration" active-text="Enable public registration" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.maintenanceMode" active-text="Maintenance mode" />
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <el-tab-pane label="Security" name="security">
          <el-form label-position="top" style="max-width: 500px">
            <el-form-item label="Session Timeout (minutes)">
              <el-input-number v-model="form.sessionTimeout" :min="5" :max="1440" />
            </el-form-item>
            <el-form-item label="Password Policy">
              <el-select v-model="form.passwordPolicy" style="width: 100%">
                <el-option label="Low (6+ chars)" value="low" />
                <el-option label="Medium (8+ chars, mixed case)" value="medium" />
                <el-option label="High (12+ chars, special chars)" value="high" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.require2FA" active-text="Require two-factor authentication" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.rateLimit" active-text="Rate limit login attempts" />
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <el-tab-pane label="Notifications" name="notifications">
          <el-form label-position="top" style="max-width: 500px">
            <el-form-item label="Admin Email">
              <el-input v-model="form.adminEmail" type="email" placeholder="admin@example.com" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.emailOnRegister" active-text="Email on new user registration" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.emailOnPluginError" active-text="Email on plugin errors" />
            </el-form-item>
            <el-form-item>
              <el-switch v-model="form.emailOnHealthChange" active-text="Email on system health changes" />
            </el-form-item>
          </el-form>
        </el-tab-pane>

        <el-tab-pane label="Plugins" name="plugins">
          <div style="max-width: 500px">
            <el-form label-position="top">
              <el-form-item label="Plugin Directory">
                <el-input v-model="form.pluginDir" placeholder="../plugins" />
              </el-form-item>
              <el-form-item>
                <el-switch v-model="form.autoDiscover" active-text="Auto-discover new plugins" />
              </el-form-item>
              <el-form-item>
                <el-switch v-model="form.hotReload" active-text="Enable hot-reload in development" />
              </el-form-item>
            </el-form>
            <el-alert title="Plugin settings require a restart to take effect" type="info" :closable="false" show-icon />
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { Check } from '@element-plus/icons-vue'
import { useSettingsStore } from '@/stores/settingsStore'
import { useNotificationStore } from '@/stores/notificationStore'

const settingsStore = useSettingsStore()
const notificationStore = useNotificationStore()

const activeTab = ref('general')

interface SettingsForm {
  siteName: string
  siteDescription: string
  enableRegistration: boolean
  maintenanceMode: boolean
  sessionTimeout: number
  passwordPolicy: string
  require2FA: boolean
  rateLimit: boolean
  adminEmail: string
  emailOnRegister: boolean
  emailOnPluginError: boolean
  emailOnHealthChange: boolean
  pluginDir: string
  autoDiscover: boolean
  hotReload: boolean
}

const initial = reactive<SettingsForm>({
  siteName: '',
  siteDescription: '',
  enableRegistration: true,
  maintenanceMode: false,
  sessionTimeout: 60,
  passwordPolicy: 'medium',
  require2FA: false,
  rateLimit: true,
  adminEmail: '',
  emailOnRegister: true,
  emailOnPluginError: true,
  emailOnHealthChange: true,
  pluginDir: '../plugins',
  autoDiscover: true,
  hotReload: true,
})

const form = reactive<SettingsForm>({ ...initial })

// Detect if the user made changes
const hasChanges = computed(() => {
  return (Object.keys(initial) as (keyof SettingsForm)[]).some((k) => form[k] !== initial[k])
})

// Map backend settings to the form
function syncFromStore() {
  if (!settingsStore.settings.length) return
  const v = (key: string, fallback = '') => settingsStore.getValue(key, fallback)

  initial.siteName = v('site.name', 'Admin Shell')
  initial.siteDescription = v('site.description', 'Admin management panel')
  initial.enableRegistration = v('site.registration', 'true') === 'true'
  initial.maintenanceMode = v('site.maintenance', 'false') === 'true'
  initial.sessionTimeout = Number(v('security.session.timeout', '60'))
  initial.passwordPolicy = v('security.password.policy', 'medium')
  initial.require2FA = v('security.2fa', 'false') === 'true'
  initial.rateLimit = v('security.rate.limit', 'true') === 'true'
  initial.adminEmail = v('notifications.admin.email', '')
  initial.emailOnRegister = v('notifications.email.on.register', 'true') === 'true'
  initial.emailOnPluginError = v('notifications.email.on.plugin.error', 'true') === 'true'
  initial.emailOnHealthChange = v('notifications.email.on.health', 'true') === 'true'
  initial.pluginDir = v('plugins.directory', '../plugins')
  initial.autoDiscover = v('plugins.auto.discover', 'true') === 'true'
  initial.hotReload = v('plugins.hot.reload', 'true') === 'true'

  // Sync to form
  Object.assign(form, initial)
}

watch(
  () => settingsStore.settings,
  () => syncFromStore(),
  { immediate: false }
)

async function saveSettings() {
  try {
    const changes: Record<string, string> = {}
    const isDiff = (key: keyof SettingsForm) => form[key] !== initial[key]

    if (isDiff('siteName')) changes['site.name'] = form.siteName
    if (isDiff('siteDescription')) changes['site.description'] = form.siteDescription
    if (isDiff('enableRegistration')) changes['site.registration'] = form.enableRegistration.toString()
    if (isDiff('maintenanceMode')) changes['site.maintenance'] = form.maintenanceMode.toString()
    if (isDiff('sessionTimeout')) changes['security.session.timeout'] = form.sessionTimeout.toString()
    if (isDiff('passwordPolicy')) changes['security.password.policy'] = form.passwordPolicy
    if (isDiff('require2FA')) changes['security.2fa'] = form.require2FA.toString()
    if (isDiff('rateLimit')) changes['security.rate.limit'] = form.rateLimit.toString()
    if (isDiff('adminEmail')) changes['notifications.admin.email'] = form.adminEmail
    if (isDiff('emailOnRegister')) changes['notifications.email.on.register'] = form.emailOnRegister.toString()
    if (isDiff('emailOnPluginError')) changes['notifications.email.on.plugin.error'] = form.emailOnPluginError.toString()
    if (isDiff('emailOnHealthChange')) changes['notifications.email.on.health'] = form.emailOnHealthChange.toString()
    if (isDiff('pluginDir')) changes['plugins.directory'] = form.pluginDir
    if (isDiff('autoDiscover')) changes['plugins.auto.discover'] = form.autoDiscover.toString()
    if (isDiff('hotReload')) changes['plugins.hot.reload'] = form.hotReload.toString()

    await settingsStore.saveAllSettings(changes)
    // Update the "initial" snapshot so hasChanges resets
    Object.assign(initial, form)
    notificationStore.addNotification('Settings saved successfully', 'success')
  } catch {
    notificationStore.addNotification('Failed to save settings', 'error')
  }
}

onMounted(() => {
  settingsStore.loadSettings()
})
</script>