<template>
  <div class="profile-page">
    <h2>My Profile</h2>

    <el-tabs v-model="activeTab">
      <el-tab-pane label="Profile" name="profile">
        <el-card shadow="never">
          <el-form
            ref="profileFormRef"
            :model="profileForm"
            :rules="profileRules"
            label-position="top"
            style="max-width: 500px;"
          >
            <el-form-item label="Username">
              <el-input v-model="profileForm.username" disabled />
            </el-form-item>
            <el-form-item label="Display Name" prop="displayName">
              <el-input v-model="profileForm.displayName" placeholder="Your display name" />
            </el-form-item>
            <el-form-item label="Email" prop="email">
              <el-input v-model="profileForm.email" placeholder="your@email.com" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="saving" @click="saveProfile">
                {{ saving ? 'Saving...' : 'Save Changes' }}
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="Password" name="password">
        <el-card shadow="never">
          <el-form
            ref="passwordFormRef"
            :model="passwordForm"
            :rules="passwordRules"
            label-position="top"
            style="max-width: 500px;"
          >
            <el-form-item label="Current Password" prop="currentPassword">
              <el-input v-model="passwordForm.currentPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="New Password" prop="newPassword">
              <el-input v-model="passwordForm.newPassword" type="password" show-password />
            </el-form-item>
            <el-form-item label="Confirm Password" prop="confirmPassword">
              <el-input v-model="passwordForm.confirmPassword" type="password" show-password />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="changingPassword" @click="changePassword">
                {{ changingPassword ? 'Changing...' : 'Change Password' }}
              </el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <!-- Plugin tabs (from ITabPlugin) -->
      <el-tab-pane
        v-for="tab in pluginTabs"
        :key="tab.id"
        :label="tab.label"
        :name="tab.id"
      >
        <el-card shadow="never">
          <el-empty :description="`${tab.label} — contributed by plugin`" />
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { authApi } from '@/services/api'
import { useAuthStore } from '@/stores/authStore'
import { useExtensionStore } from '@/stores/extensionStore'

const authStore = useAuthStore()
const extensionStore = useExtensionStore()

const activeTab = ref('profile')
const saving = ref(false)
const changingPassword = ref(false)
const profileFormRef = ref()
const passwordFormRef = ref()

// Plugin tabs targeting 'profile'
const pluginTabs = computed(() =>
  extensionStore.getTabsForPage('profile')
)

const profileForm = reactive({
  username: authStore.user?.username ?? '',
  displayName: authStore.user?.displayName ?? '',
  email: authStore.user?.email ?? '',
})

const profileRules = {
  email: [
    { required: true, message: 'Email is required', trigger: 'blur' },
    { type: 'email', message: 'Invalid email format', trigger: 'blur' },
  ],
  displayName: [{ min: 2, message: 'Min 2 characters', trigger: 'blur' }],
}

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const validateConfirm = (_rule: any, value: string, callback: any) => {
  if (value !== passwordForm.newPassword) {
    callback(new Error('Passwords do not match'))
  } else {
    callback()
  }
}

const passwordRules = {
  currentPassword: [{ required: true, message: 'Current password required', trigger: 'blur' }],
  newPassword: [
    { required: true, message: 'New password required', trigger: 'blur' },
    { min: 6, message: 'Min 6 characters', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: 'Confirm password required', trigger: 'blur' },
    { validator: validateConfirm, trigger: 'blur' },
  ],
}

async function saveProfile() {
  const valid = await profileFormRef.value?.validate().catch(() => false)
  if (!valid) return
  saving.value = true
  try {
    const res = await authApi.put('/api/auth/profile', {
      displayName: profileForm.displayName,
      email: profileForm.email,
    })
    if (res?.data) {
      authStore.setUser(res.data)
      ElMessage.success('Profile updated')
    }
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || 'Failed to update profile')
  } finally {
    saving.value = false
  }
}

async function changePassword() {
  const valid = await passwordFormRef.value?.validate().catch(() => false)
  if (!valid) return
  changingPassword.value = true
  try {
    await authApi.post('/api/auth/change-password', {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword,
    })
    ElMessage.success('Password changed')
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''
  } catch (e: any) {
    ElMessage.error(e?.response?.data?.message || 'Failed to change password')
  } finally {
    changingPassword.value = false
  }
}

onMounted(async () => {
  // Load user data
  try {
    const res = await authApi.get('/api/auth/me')
    if (res?.data) {
      profileForm.displayName = res.data.displayName ?? ''
      profileForm.email = res.data.email ?? ''
      profileForm.username = res.data.username ?? ''
      authStore.setUser(res.data)
    }
  } catch {
    // Use cached user
  }
})
</script>

<style scoped>
.profile-page {
  padding: 24px;
}

.profile-page h2 {
  margin: 0 0 20px;
}

/* ===== Mobile Adjustments ===== */
@media (max-width: 768px) {
  .profile-page {
    padding: 16px;
  }
}

@media (max-width: 480px) {
  .profile-page :deep(.el-form--label-top .el-form-item__label) {
    padding-bottom: 4px;
  }
}
</style>