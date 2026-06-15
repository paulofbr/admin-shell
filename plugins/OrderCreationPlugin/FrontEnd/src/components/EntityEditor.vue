<script setup lang="ts">
import { computed } from 'vue'
import { ElButton } from 'element-plus'

type ButtonType = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default'

const props = withDefaults(defineProps<{
  title: string
  subtitle?: string
  loading?: boolean
  saveLabel?: string
  cancelLabel?: string
  showCancel?: boolean
  showSave?: boolean
  saveType?: ButtonType
  saveDisabled?: boolean
  saveLoading?: boolean
  onSave?: () => void | Promise<void>
  onCancel?: () => void
}>(), {
  subtitle: undefined,
  loading: false,
  saveLabel: 'Save',
  cancelLabel: 'Cancel',
  showCancel: true,
  showSave: true,
  saveType: 'primary',
  saveDisabled: undefined,
  saveLoading: false,
  onSave: undefined,
  onCancel: undefined,
})

const emit = defineEmits<{
  save: []
  cancel: []
}>()

const hasFooterActions = computed(() => props.showCancel || props.showSave)
const isSaving = computed(() => props.saveLoading || props.loading)

async function handleSave() {
  if (props.onSave) {
    await props.onSave()
    return
  }

  emit('save')
}

function handleCancel() {
  if (props.onCancel) {
    props.onCancel()
    return
  }

  emit('cancel')
}
</script>

<template>
  <section class="entity-editor" :class="{ 'entity-editor--loading': loading }">
    <div class="entity-editor__toolbar">
      <div class="entity-editor__heading">
        <p v-if="subtitle" class="entity-editor__subtitle">
          {{ subtitle }}
        </p>
        <h2 class="entity-editor__title">
          {{ title }}
        </h2>
      </div>

      <div v-if="$slots['toolbar-actions']" class="entity-editor__toolbar-actions">
        <slot name="toolbar-actions" />
      </div>
    </div>

    <div v-if="loading" class="entity-editor__loading">
      <el-button :loading="true" />
    </div>

    <div v-else class="entity-editor__content">
      <slot />
    </div>

    <div v-if="hasFooterActions" class="entity-editor__footer">
      <slot name="actions">
        <el-button
          v-if="showCancel"
          type="default"
          :disabled="isSaving"
          @click="handleCancel"
        >
          {{ cancelLabel }}
        </el-button>

        <el-button
          v-if="showSave"
          :type="saveType"
          :disabled="saveDisabled"
          :loading="isSaving"
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
  gap: 18px;
  width: 100%;
}

.entity-editor__toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.entity-editor__heading {
  min-width: 0;
}

.entity-editor__subtitle {
  margin: 0 0 4px;
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.entity-editor__title {
  margin: 0;
  color: var(--el-text-color-primary);
  font-size: 20px;
  line-height: 1.25;
}

.entity-editor__toolbar-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.entity-editor__content {
  display: grid;
  gap: 16px;
}

.entity-editor__loading {
  display: grid;
  min-height: 120px;
  place-items: center;
}

.entity-editor__footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-lighter);
}

@media (max-width: 768px) {
  .entity-editor__toolbar,
  .entity-editor__footer {
    align-items: stretch;
    flex-direction: column;
  }

  .entity-editor__footer > :deep(.el-button),
  .entity-editor__footer > :deep(button) {
    width: 100%;
  }
}
</style>
