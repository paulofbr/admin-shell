<script setup lang="ts">
import { computed } from 'vue'
import { ElFormItem, ElInput, ElSelect, ElOption, ElSwitch, ElDatePicker } from 'element-plus'
import type { FormItemRule } from 'element-plus'
import type { ExtensionField } from '../types'

const props = withDefaults(defineProps<{
  field: ExtensionField
  modelValue?: unknown
  rules?: FormItemRule[]
  prop?: string
  setValue?: (value: unknown) => void
}>(), {
  modelValue: undefined,
  rules: undefined,
  prop: undefined,
  setValue: undefined,
})

const fieldType = computed(() => props.field.type.toLowerCase())

const booleanValue = computed(() => {
  if (typeof props.modelValue === 'boolean') return props.modelValue
  if (typeof props.modelValue === 'string') return ['1', 'true', 'yes', 'on'].includes(props.modelValue.toLowerCase())
  return Boolean(props.modelValue)
})

function updateValue(value: unknown) {
  props.setValue?.(value)
}
</script>

<template>
  <el-form-item
    :label="field.label || field.name"
    :prop="prop"
    :rules="rules"
  >
    <el-select
      v-if="fieldType === 'select' && field.possibleValues?.values?.length"
      :model-value="modelValue"
      :placeholder="`Select ${field.label || field.name}`"
      clearable
      @update:model-value="updateValue"
    >
      <el-option
        v-for="option in field.possibleValues.values"
        :key="option"
        :label="option"
        :value="option"
      />
    </el-select>

    <el-switch
      v-else-if="fieldType === 'boolean'"
      :model-value="booleanValue"
      @update:model-value="updateValue"
    />

    <el-input
      v-else-if="fieldType === 'textarea' || fieldType === 'text'"
      :model-value="modelValue"
      type="textarea"
      :placeholder="field.label || field.name"
      :rows="4"
      @update:model-value="updateValue"
    />

    <el-input
      v-else-if="fieldType === 'number'"
      :model-value="modelValue"
      type="number"
      :placeholder="field.label || field.name"
      @update:model-value="value => updateValue(value === '' || value === null || value === undefined ? null : Number(value))"
    />

    <el-date-picker
      v-else-if="fieldType === 'date'"
      :model-value="modelValue"
      type="date"
      :placeholder="field.label || field.name"
      @update:model-value="updateValue"
    />

    <el-date-picker
      v-else-if="fieldType === 'datetime'"
      :model-value="modelValue"
      type="datetime"
      :placeholder="field.label || field.name"
      @update:model-value="updateValue"
    />

    <el-input
      v-else
      :model-value="modelValue"
      :placeholder="field.label || field.name"
      @update:model-value="updateValue"
    />
  </el-form-item>
</template>
