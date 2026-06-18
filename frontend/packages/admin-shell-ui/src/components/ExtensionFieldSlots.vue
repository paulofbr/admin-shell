<script setup lang="ts">
import { computed } from 'vue'
import type { FormRules } from 'element-plus'
import ExtensionFieldFormItem from './ExtensionFieldFormItem.vue'
import type { ExtensionField } from '../types'

const props = withDefaults(defineProps<{
  extensionFields?: ExtensionField[]
}>(), {
  extensionFields: () => []
})

const extensionSlots = computed(() =>
  Array.from(new Set(props.extensionFields
    .map(field => field.slot)
    .filter((slot): slot is string => Boolean(slot)))),
)

const unslottedExtensionFields = computed(() =>
  props.extensionFields
    .filter(field => !field.slot)
    .sort((a, b) => a.order - b.order || a.name.localeCompare(b.name)),
)

function getFieldsForSlot(slotName: string) {
  return props.extensionFields
    .filter(field => field.slot === slotName)
    .sort((a, b) => a.order - b.order || a.name.localeCompare(b.name))
}

function extensionProp(field: ExtensionField) {
  return `extensionFields.${field.name}`
}

function extensionRules(field: ExtensionField): FormRules[string] {
  if (!field.required) return []
  return [{ required: true, message: `${field.label || field.name} is required`, trigger: 'blur' }]
}

function setExtensionField(field: ExtensionField, value: unknown) {
  field.value = value
}
</script>

<template>
  <template v-for="slotName in extensionSlots" :key="slotName">
    <ExtensionFieldFormItem
      v-for="field in getFieldsForSlot(slotName)"
      :key="field.name"
      :field="field"
      :model-value="field.value"
      :rules="extensionRules(field)"
      :prop="extensionProp(field)"
      :set-value="value => setExtensionField(field, value)"
    />
  </template>

  <ExtensionFieldFormItem
    v-for="field in unslottedExtensionFields"
    :key="field.name"
    :field="field"
    :model-value="field.value"
    :rules="extensionRules(field)"
    :prop="extensionProp(field)"
    :set-value="value => setExtensionField(field, value)"
  />
</template>
