<script setup lang="ts">
import { computed, ref } from 'vue'
import EntityEditor from '@admin-shell/ui/EntityEditor.vue'
import * as rolesApi from '@/api/roles'
import type { FormRules } from 'element-plus'
import type { Role } from '@admin-shell/ui/types'

type ResponsiveSize = {
  mobile?: string
  desktop?: string
  breakpoint?: number
}

interface Props {
  modelValue?: Role | null
  close?: () => void
}

const defaultSize: ResponsiveSize = {
  mobile: '95%',
  desktop: '480px',
}

const props = defineProps<Props>()
const emit = defineEmits<{
  saved: [value: Role]
}>()

const editorRef = ref<InstanceType<typeof EntityEditor>>()
const isEditing = computed(() => editorRef.value?.isEditing.value ?? false)
const formModel = computed(() => editorRef.value?.formModel.value as Role)
const editorTitle = computed(() => (isEditing.value ? 'Edit Role' : 'Add Role'))

function createEmptyRole(): Record<string, unknown> {
  return {
    id: '',
    name: '',
    description: '',
    createdAt: new Date().toISOString(),
    extensionFields: [],
  }
}

const formRules: FormRules = {
  name: [{ required: true, message: 'Name is required', trigger: 'blur' }],
}

async function save() {
  return isEditing.value && (formModel.value.id as string)
    ? await rolesApi.updateRole(formModel.value.id as string, formModel.value as Role)
    : await rolesApi.createRole(formModel.value as Parameters<typeof rolesApi.createRole>[0])
}
</script>

<template>
  <EntityEditor
    ref="editorRef"
    :model-value="props.modelValue"
    :empty-model="createEmptyRole"
    :form-rules="formRules"
    :save-success-message="isEditing ? 'Role updated' : 'Role created'"
    save-error-message="Failed to save role"
    label-position="top"
    :title="editorTitle"
    :default-size="defaultSize"
    :save-label="isEditing ? 'Save Changes' : 'Create Role'"
    :save-handler="save"
    :on-cancel="close"
  >
    <el-form-item label="Name" prop="name">
      <el-input v-model="formModel.name" placeholder="e.g. Editor" />
    </el-form-item>
    <el-form-item label="Description" prop="description">
      <el-input v-model="formModel.description" placeholder="Optional description" type="textarea" :rows="2" />
    </el-form-item>
  </EntityEditor>
</template>
