<script setup lang="ts">
import { computed, ref, watch } from 'vue'
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

const isEditing = ref(false)
const form = ref<Role>({
  id: '',
  name: '',
  description: '',
  createdAt: new Date().toISOString(),
  extensionFields: [],
})
const editorTitle = computed(() => (isEditing.value ? 'Edit Role' : 'Add Role'))

const formRules: FormRules = {
  name: [{ required: true, message: 'Name is required', trigger: 'blur' }],
}

watch(
  () => props.modelValue,
  (role) => {
    isEditing.value = !!role
  },
  { immediate: true },
)

async function save() {
  return isEditing.value && form.value.id
    ? await rolesApi.updateRole(form.value.id, form.value)
    : await rolesApi.createRole(form.value)
}
</script>

<template>
  <EntityEditor
    :source-model="props.modelValue"
    :form-model="form"
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
      <el-input v-model="form.name" placeholder="e.g. Editor" />
    </el-form-item>
    <el-form-item label="Description" prop="description">
      <el-input v-model="form.description" placeholder="Optional description" type="textarea" :rows="2" />
    </el-form-item>
  </EntityEditor>
</template>
