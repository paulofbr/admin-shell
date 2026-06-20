<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { ElButton, ElForm } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import ExtensionFieldSlots from './ExtensionFieldSlots.vue'
import { useNotificationStore } from '../stores/notificationStore'
import type { ExtensionField, ValidationResult } from '../types'

export interface ResponsiveSize {
  mobile?: string
  desktop?: string
  breakpoint?: number
}

type ButtonType = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default'

type EmptyModelFactory = () => Record<string, unknown>


const props = withDefaults(defineProps<{
  title?: string
  subtitle?: string
  eyebrow?: string
  defaultSize?: string | ResponsiveSize
  sourceModel?: unknown
  modelValue?: unknown
  emptyModel?: Record<string, unknown> | EmptyModelFactory
  loading?: boolean
  showCancel?: boolean
  showSave?: boolean
  saveLabel?: string
  cancelLabel?: string
  saveType?: ButtonType
  saveDisabled?: boolean
  saveLoading?: boolean
  formModel?: Record<string, unknown>
  formRules?: FormRules
  validateExtensionFields?: boolean
  saveSuccessMessage?: string | ((value: unknown) => string)
  saveErrorMessage?: string
  labelPosition?: 'top' | 'right' | 'left'
  validate?: () => Promise<boolean> | boolean
  saveHandler?: (value: unknown) => Promise<unknown> | unknown
  onCancel?: () => void
  close?: () => void
}>(), {
  title: 'Edit',
  loading: false,
  showCancel: true,
  showSave: true,
  saveLabel: 'Save',
  cancelLabel: 'Cancel',
  saveType: 'primary',
  saveDisabled: false,
  saveLoading: false,
  validateExtensionFields: false,
  saveErrorMessage: 'Failed to save entity',
  labelPosition: 'top'
})

const windowWidth = ref(typeof window === 'undefined' ? 1024 : window.innerWidth)

function syncWindowWidth(): void {
  windowWidth.value = typeof window === 'undefined' ? 1024 : window.innerWidth
}

function resolveResponsiveSize(value: string | ResponsiveSize | undefined): string {
  if (!value) return "500px";

  if (typeof value === 'string') {
    return windowWidth.value < 768 ? "95%" : value;
  }

  const breakpoint = value.breakpoint ?? 768;
  return windowWidth.value < breakpoint
    ? value.mobile ?? "95%"
    : value.desktop ?? "500px";
}

onMounted(() => {
  syncWindowWidth()
  window.addEventListener('resize', syncWindowWidth)
})

onUnmounted(() => {
  window.removeEventListener('resize', syncWindowWidth)
})

const resolvedWidth = computed(() => resolveResponsiveSize(props.defaultSize))

const effectiveSourceModel = computed(() => (
  props.sourceModel !== undefined ? props.sourceModel : props.modelValue
))

const internalFormModel = ref<Record<string, unknown>>({})
const formModel = computed(() => props.formModel ?? internalFormModel.value)

const extensionFields = computed(() => {
  const model = formModel.value as { extensionFields?: ExtensionField[] } | undefined
  return model?.extensionFields ?? []
})

const isEditing = computed(() => !!effectiveSourceModel.value)

function resolveEmptyModel(): Record<string, unknown> {
  if (!props.emptyModel) return {}

  if (typeof props.emptyModel === 'function') {
    return (props.emptyModel as EmptyModelFactory)()
  }

  return { ...props.emptyModel }
}

function createFreshModel(value: unknown): Record<string, unknown> {
  if (value && typeof value === 'object') {
    return { ...(value as Record<string, unknown>) }
  }

  return resolveEmptyModel()
}

function syncFormModel(value: unknown): void {
  const target = formModel.value
  const freshModel = createFreshModel(value)

  Object.keys(target).forEach((key) => {
    if (!(key in freshModel)) {
      delete target[key]
    }
  })

  Object.assign(target, freshModel)
}

watch(
  () => [effectiveSourceModel.value, props.emptyModel],
  () => syncFormModel(effectiveSourceModel.value),
  { immediate: true },
)

const emit = defineEmits<{
  cancel: []
  save: [value: unknown]
  saved: [value: unknown]
}>()

const notificationStore = useNotificationStore()

defineExpose({
  isEditing,
  formModel,
  submit: handleSave,
})

const isSaving = computed(() => props.saveLoading || props.loading || saving.value)
const duplicateActionWindowMs = 100
let isHandlingSave = false
let isHandlingCancel = false
let lastSaveAt = 0
let lastCancelAt = 0

const formRef = ref<FormInstance>()
const saving = ref(false)

async function validateExtensionFields(): Promise<boolean> {
  for (const field of extensionFields.value) {
    if (field.required && (field.value === undefined || field.value === null || field.value === '')) {
      const message = `${field.label || field.name} is required`
      notificationStore.addNotification(message, 'error')
      return false
    }

    if (!field.frontEndValidator) continue

    try {
      const validator = new Function('entity', `return (${field.frontEndValidator});`) as (
        entity: unknown
      ) => ValidationResult | Promise<ValidationResult>
      const result = await validator(formModel.value)

      if (result?.ok === false) {
        const message = result.message ?? 'Extension field validation failed'
        notificationStore.addNotification(message, 'error')
        return false
      }
    } catch {
      // Ignore invalid custom validator code and let the form handle normal rules.
    }
  }

  return true
}

async function validateForm(): Promise<boolean> {
  if (props.validate) {
    const valid = await props.validate()
    if (valid === false) return false
  }

  if (props.validateExtensionFields) {
    return validateExtensionFields()
  }

  return true
}

async function handleSave(): Promise<void> {
  const now = Date.now()
  if (isHandlingSave || now - lastSaveAt < duplicateActionWindowMs) return
  lastSaveAt = now
  isHandlingSave = true
  saving.value = true
  try {
    const isValid = await validateForm()
    if (!isValid) return

    if (props.saveHandler) {
      const result = await props.saveHandler(formModel.value)
      const savedValue = result ?? effectiveSourceModel.value
      emit('save', formModel.value)
      emit('saved', savedValue)
      notifySaveSuccess(savedValue)
      props.close?.()
      return
    }

    emit('save', formModel.value)
  } catch (error) {
    notifySaveError(error)
  } finally {
    saving.value = false
    window.setTimeout(() => {
      if (Date.now() - lastSaveAt >= duplicateActionWindowMs) {
        isHandlingSave = false
      }
    }, duplicateActionWindowMs)
  }
}

function notifySaveSuccess(value: unknown): void {
  if (!props.saveSuccessMessage) return

  const message = typeof props.saveSuccessMessage === 'function'
    ? props.saveSuccessMessage(value)
    : props.saveSuccessMessage

  notificationStore.addNotification(message, 'success')
}

function notifySaveError(error: unknown): void {
  notificationStore.addNotification(props.saveErrorMessage ?? 'Failed to save entity', 'error')
}

function handleCancel(): void {
  const now = Date.now()
  if (isHandlingCancel || now - lastCancelAt < duplicateActionWindowMs) return
  lastCancelAt = now
  isHandlingCancel = true
  try {
    if (props.onCancel) {
      props.onCancel()
      return
    }

    if (props.close) {
      props.close()
      return
    }

    emit('cancel')
  } finally {
    window.setTimeout(() => {
      if (Date.now() - lastCancelAt >= duplicateActionWindowMs) {
        isHandlingCancel = false
      }
    }, duplicateActionWindowMs)
  }
}
</script>

<template>
  <section class="entity-editor" :style="{ maxWidth: resolvedWidth }" :class="{ 'entity-editor--loading': loading }">
    <div class="entity-editor__toolbar">
      <div class="entity-editor__heading">
        <p v-if="eyebrow" class="entity-editor__eyebrow">
          {{ eyebrow }}
        </p>
        <h2 class="entity-editor__title">
          {{ title }}
        </h2>
        <p v-if="subtitle" class="entity-editor__subtitle">
          {{ subtitle }}
        </p>
      </div>

      <div class="entity-editor__toolbar-actions">
        <slot name="toolbar-actions" />
      </div>
    </div>

    <div class="entity-editor__content">
      <el-form
        v-if="formModel !== undefined || formRules !== undefined"
        ref="formRef"
        :model="formModel"
        :rules="formRules"
        :label-position="labelPosition ?? 'top'"
      >
        <slot />
      </el-form>

      <slot v-else />
    </div>

    <ExtensionFieldSlots :extension-fields="extensionFields" />

    <div v-if="showCancel || showSave" class="entity-editor__footer">
      <slot name="actions">
        <el-button
          v-if="showCancel"
          type="default"
          :disabled="loading"
          @click="handleCancel"
        >
          {{ cancelLabel }}
        </el-button>

        <el-button
          v-if="showSave"
          :type="props.saveType"
          :loading="isSaving"
          :disabled="saveDisabled || loading"
          @click="handleSave"
        >
          {{ saveLabel }}
        </el-button>
      </slot>
    </div>
  </section>
</template>

<style scoped>
.entity-editor {
  display: grid;
  gap: 20px;
  min-width: 0;
}

.entity-editor__toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 18px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.entity-editor__heading {
  display: grid;
  gap: 6px;
  min-width: 0;
}

.entity-editor__eyebrow,
.entity-editor__subtitle {
  margin: 0;
  color: var(--el-text-color-secondary);
  font-size: 13px;
  line-height: 1.5;
}

.entity-editor__eyebrow {
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.entity-editor__title {
  margin: 0;
  color: var(--el-text-color-primary);
  font-size: 20px;
  font-weight: 700;
  line-height: 1.25;
}

.entity-editor__subtitle {
  max-width: 760px;
}

.entity-editor__toolbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.entity-editor__content {
  min-width: 0;
}

.entity-editor__content :deep(.el-form) {
  min-width: 0;
}

.entity-editor__footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 18px;
  border-top: 1px solid var(--el-border-color-lighter);
}

@media (max-width: 768px) {
  .entity-editor__toolbar {
    align-items: stretch;
    flex-direction: column;
  }

  .entity-editor__toolbar-actions {
    justify-content: flex-end;
    flex-wrap: wrap;
  }

  .entity-editor__footer {
    justify-content: stretch;
    flex-direction: column-reverse;
  }

  .entity-editor__footer .el-button {
    width: 100%;
  }
}
</style>
