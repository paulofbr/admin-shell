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
            Modular administration panel with plugin-based extensibility.
            Manage users, plugins, and system settings from one place.
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
            <h2 class="auth-form__title">Welcome back</h2>
            <p class="auth-form__subtitle">Sign in to your account to continue</p>
          </div>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            class="auth-form"
            @submit.prevent="handleSubmit"
          >
            <el-form-item label="Email" prop="email">
              <el-input
                v-model="form.email"
                type="email"
                placeholder="you@example.com"
                :prefix-icon="Message"
                size="large"
                autocomplete="email"
              />
            </el-form-item>

            <el-form-item label="Password" prop="password">
              <el-input
                v-model="form.password"
                type="password"
                placeholder="••••••••"
                :prefix-icon="Lock"
                size="large"
                show-password
                autocomplete="current-password"
              />
            </el-form-item>

            <div class="auth-form__options">
              <el-checkbox v-model="rememberMe">Remember me</el-checkbox>
              <el-link type="primary">Forgot password?</el-link>
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
                {{ isSubmitting ? 'Signing in...' : 'Sign In' }}
              </el-button>
            </el-form-item>
          </el-form>

          <p class="auth-form__footer">
            Don't have an account?
            <router-link to="/register">Create one</router-link>
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
import { Message, Lock, Grid, User, Connection } from '@element-plus/icons-vue'
import type { FormInstance, FormRules } from 'element-plus'

const router = useRouter()
const authStore = useAuthStore()
const notificationStore = useNotificationStore()

const formRef = ref<FormInstance>()
const isSubmitting = ref(false)
const rememberMe = ref(true)

const form = reactive({
  email: 'admin@admin.com',
  password: 'admin123',
})

const rules: FormRules = {
  email: [
    { required: true, message: 'Please enter your email', trigger: 'blur' },
    { type: 'email', message: 'Please enter a valid email', trigger: 'blur' },
  ],
  password: [
    { required: true, message: 'Please enter your password', trigger: 'blur' },
  ],
}

async function handleSubmit() {
  if (!formRef.value) return
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  isSubmitting.value = true
  try {
    await authStore.login(form.email, form.password)
    notificationStore.addNotification('Login successful', 'success')
    router.push('/')
  } catch (error: unknown) {
    const message =
      error instanceof Error ? error.message : 'Invalid email or password'
    notificationStore.addNotification(message, 'error')
  } finally {
    isSubmitting.value = false
  }
}
</script>