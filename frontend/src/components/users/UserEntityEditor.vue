<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import EntityEditor from '@admin-shell/ui/EntityEditor.vue'
import * as usersApi from '@/api/users'
import type { User } from '@admin-shell/ui/types'

interface Props {
  modelValue?: User | null
  close?: () => void
}


const props = defineProps<Props>()
const emit = defineEmits<{
  saved: [value: User]
}>()

const isEditing = ref(false)
const form = ref<User>({
  id: '',
  email: '',
  username: '',
  displayName: '',
  password: '',
  isActive: true,
  createdAt: new Date().toISOString(),
  extensionFields: [],
})

const editorTitle = computed(() => (isEditing.value ? 'Edit User' : 'Add User'))

watch(
  () => props.modelValue,
  (user) => {
    isEditing.value = !!user
  },
  { immediate: true },
)

async function save() {
  return isEditing.value && form.value.id
    ? await usersApi.updateUser(form.value.id, form.value)
    : await usersApi.createUser(form.value)
}
</script>

<template>
  <EntityEditor
    :source-model="props.modelValue"
    :form-model="form"
    :save-success-message="isEditing ? 'User updated' : 'User created'"
    save-error-message="Failed to save user"
    :title="editorTitle"
    :default-size="defaultSize"
    :save-label="isEditing ? 'Save Changes' : 'Create User'"
    :save-handler="save"
    :on-cancel="close"
  >
    <el-form-item label="Email" prop="email">
      <el-input v-model="form.email" placeholder="user@example.com" />
    </el-form-item>
    <el-row :gutter="16" class="user-form-row">
      <el-col :span="12" class="user-form-col">
        <el-form-item label="Username" prop="username">
          <el-input v-model="form.username" placeholder="johndoe" />
        </el-form-item>
      </el-col>
      <el-col :span="12" class="user-form-col">
        <el-form-item label="Display Name" prop="displayName">
          <el-input v-model="form.displayName" placeholder="John Doe" />
        </el-form-item>
      </el-col>
    </el-row>
    <el-form-item v-if="!isEditing" label="Password" prop="password">
      <el-input
        v-model="form.password"
        type="password"
        show-password
        placeholder="Enter password"
      />
    </el-form-item>
    <el-form-item v-if="isEditing" label="Active" prop="isActive">
      <el-switch v-model="form.isActive" />
    </el-form-item>
  </EntityEditor>
</template>
