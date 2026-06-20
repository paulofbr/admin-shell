<script setup lang="ts">
import { computed, ref } from 'vue'
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

const editorRef = ref<InstanceType<typeof EntityEditor>>()
const isEditing = computed(() => editorRef.value?.isEditing.value ?? false)
const formModel = computed(() => editorRef.value?.formModel.value as User & { password: string })
const editorTitle = computed(() => (isEditing.value ? 'Edit User' : 'Add User'))

function createEmptyUser(): Record<string, unknown> {
  return {
    id: '',
    email: '',
    username: '',
    displayName: '',
    password: '',
    isActive: true,
    createdAt: new Date().toISOString(),
    extensionFields: [],
  }
}

async function save() {
  return isEditing.value && (formModel.value.id as string)
    ? await usersApi.updateUser(formModel.value as User)
    : await usersApi.createUser(formModel.value as Parameters<typeof usersApi.createUser>[0])
}
</script>

<template>
  <EntityEditor
    ref="editorRef"
    :model-value="props.modelValue"
    :empty-model="createEmptyUser"
    :save-success-message="isEditing ? 'User updated' : 'User created'"
    save-error-message="Failed to save user"
    :title="editorTitle"
    :default-size="defaultSize"
    :save-label="isEditing ? 'Save Changes' : 'Create User'"
    :save-handler="save"
    :on-cancel="close"
  >
    <el-form-item label="Email" prop="email">
      <el-input v-model="formModel.email" placeholder="user@example.com" />
    </el-form-item>
    <el-row :gutter="16" class="user-form-row">
      <el-col :span="12" class="user-form-col">
        <el-form-item label="Username" prop="username">
          <el-input v-model="formModel.username" placeholder="johndoe" />
        </el-form-item>
      </el-col>
      <el-col :span="12" class="user-form-col">
        <el-form-item label="Display Name" prop="displayName">
          <el-input v-model="formModel.displayName" placeholder="John Doe" />
        </el-form-item>
      </el-col>
    </el-row>
    <el-form-item v-if="!isEditing" label="Password" prop="password">
      <el-input
        v-model="formModel.password"
        type="password"
        show-password
        placeholder="Enter password"
      />
    </el-form-item>
    <el-form-item v-if="isEditing" label="Active" prop="isActive">
      <el-switch v-model="formModel.isActive" />
    </el-form-item>
  </EntityEditor>
</template>
