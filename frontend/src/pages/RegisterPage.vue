<template>
  <div class="auth-page">
    <div class="auth-container">
      <!-- Left side - Branding -->
      <div class="auth-brand">
        <div class="auth-brand__content">
          <div class="auth-brand__logo">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="3" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="3" width="7" height="7" rx="1" />
              <rect x="3" y="14" width="7" height="7" rx="1" />
              <rect x="14" y="14" width="7" height="7" rx="1" />
            </svg>
          </div>
          <h1 class="auth-brand__title">Admin Shell</h1>
          <p class="auth-brand__desc">
            Join the platform. Create your account and start managing your plugins, users, and settings.
          </p>
          <div class="auth-brand__features">
            <div class="auth-brand__feature">
              <el-icon :size="16"><Grid /></el-icon>
              <span>Plugin architecture</span>
            </div>
            <div class="auth-brand__feature">
              <el-icon :size="16"><User /></el-icon>
              <span>User management</span>
            </div>
            <div class="auth-brand__feature">
              <el-icon :size="16"><Connection /></el-icon>
              <span>Extensible UI</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right side - Form -->
      <div class="auth-form-wrapper">
        <div class="auth-form-container">
          <div class="auth-form__header">
            <h2 class="auth-form__title">Create account</h2>
            <p class="auth-form__subtitle">Fill in the details to get started</p>
          </div>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            class="auth-form"
            @submit.prevent="handleSubmit"
          >
            <el-form-item label="Email *" prop="email">
              <el-input
                v-model="form.email"
                type="email"
                placeholder="you@example.com"
                :prefix-icon="Message"
                size="large"
                autocomplete="email"
              />
            </el-form-item>

            <el-form-item label="Username *" prop="username">
              <el-input
                v-model="form.username"
                placeholder="johndoe"
                :prefix-icon="User"
                size="large"
                autocomplete="username"
              />
            </el-form-item>

            <el-form-item label="Display Name">
              <el-input
                v-model="form.displayName"
                placeholder="John Doe"
                size="large"
              />
            </el-form-item>

            <div class="register-password-fields">
              <el-form-item label="Password *" prop="password">
                <el-input
                  v-model="form.password"
                  type="password"
                  placeholder="••••••••"
                  :prefix-icon="Lock"
                  size="large"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>

              <el-form-item label="Confirm *" prop="confirmPassword">
                <el-input
                  v-model="form.confirmPassword"
                  type="password"
                  placeholder="••••••••"
                  :prefix-icon="Lock"
                  size="large"
                  show-password
                  autocomplete="new-password"
                />
              </el-form-item>
            </div>

            <el-form-item>
              <el-button
                type="primary"
                native-type="submit"
                :loading="isSubmitting"
                size="large"
                style="width: 100%"
                round
              >
                {{ isSubmitting ? 'Creating account...' : 'Create Account' }}
              </el-button>
            </el-form-item>
          </el-form>

          <p class="auth-form__footer">
            Already have an account?
            <router-link to="/login">Sign in</router-link>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useNotificationStore } from '@/stores/notificationStore'
import { Message, User, Lock, Grid, Connection } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const authStore = useAuthStore()
const notificationStore = useNotificationStore()

const formRef = ref<FormInstance>()
const isSubmitting = ref(false)

const form = reactive({
  email: '',
  username: '',
  password: '',
  confirmPassword: '',
  displayName: '',
})

const validateConfirm = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (value !== form.password) {
    callback(new Error('Passwords do not match'))
  } else {
    callback()
  }
}

const rules: FormRules = {
  email: [
    { required: true, message: 'Please enter your email', trigger: 'blur' },
    { type: 'email', message: 'Please enter a valid email', trigger: 'blur' },
  ],
  username: [
    { required: true, message: 'Please enter a username', trigger: 'blur' },
    { min: 3, message: 'Username must be at least 3 characters', trigger: 'blur' },
  ],
  password: [
    { required: true, message: 'Please enter a password', trigger: 'blur' },
    { min: 6, message: 'Password must be at least 6 characters', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: 'Please confirm your password', trigger: 'blur' },
    { validator: validateConfirm, trigger: 'blur' },
  ],
}

async function handleSubmit() {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  isSubmitting.value = true
  try {
    await authStore.register(
      form.email,
      form.username,
      form.password,
      form.displayName.trim() || undefined,
    )
    notificationStore.addNotification('Registration successful', 'success')
    router.push('/')
  } catch (error: unknown) {
    const message =
      error instanceof Error ? error.message : 'Registration failed'
    notificationStore.addNotification(message, 'error')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.register-password-fields {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

@media (max-width: 768px) {
  .register-password-fields {
    grid-template-columns: 1fr;
  }
}
</style>