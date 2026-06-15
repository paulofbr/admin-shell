<template>
  <div class="responsive-grid" :data-edit-mode="editMode">
    <div v-if="showToolbar" class="responsive-grid__toolbar">
      <slot name="toolbar" />

      <div v-if="filters.length > 0" class="responsive-grid__filters">
        <el-select
          v-for="filter in selectFilters"
          :key="filter.id"
          :model-value="filterValues[filter.id]"
          :placeholder="filter.placeholder ?? filter.label"
          clearable
          class="responsive-grid__filter"
          @update:model-value="updateFilter(filter.id, $event)"
        >
          <el-option
            v-for="option in filter.options"
            :key="option.value"
            :label="option.label"
            :value="option.value"
          />
        </el-select>

        <el-input
          v-for="filter in textFilters"
          :key="filter.id"
          :model-value="filterValues[filter.id]"
          :placeholder="filter.placeholder ?? filter.label"
          clearable
          class="responsive-grid__filter"
          @update:model-value="updateFilter(filter.id, $event)"
        />
      </div>
    </div>

    <div class="responsive-grid__table" data-responsive-grid-table>
      <el-table
        v-loading="loading"
        :data="data"
        :row-key="resolveRowKey"
        stripe
        style="width: 100%"
        class="responsive-grid__desktop-table"
      >
        <el-table-column
          v-for="column in columns"
          :key="column.id"
          :prop="column.prop"
          :label="column.label"
          :width="column.width"
          :min-width="column.minWidth"
          :align="column.align"
          :fixed="column.fixed"
          :sortable="column.sortable"
        >
          <template #header="headerScope">
            <slot
              v-if="$slots[`header-${column.id}`]"
              :name="`header-${column.id}`"
              :column="column"
              :scope="headerScope"
            />
            <span v-else>{{ column.label }}</span>
          </template>

          <template #default="cellScope">
            <slot
              :name="`cell-${column.id}`"
              :row="cellScope.row as GridRow"
              :column="column"
              :value="getCellValue(cellScope.row as GridRow, column)"
            >
              <span class="responsive-grid__cell-value">
                {{ formatCellValue(cellScope.row as GridRow, column) }}
              </span>
            </slot>
          </template>
        </el-table-column>

        <template #empty>
          <slot name="empty">
            <div class="responsive-grid__empty">
              {{ emptyText }}
            </div>
          </slot>
        </template>
      </el-table>
    </div>

    <div v-if="data.length > 0" class="responsive-grid__mobile-cards" data-responsive-grid-mobile-cards>
      <article
        v-for="row in data"
        :key="resolveRowKey(row)"
        class="responsive-grid__mobile-card"
      >
        <div
          v-for="field in computedMobileFields"
          :key="field.id"
          class="responsive-grid__mobile-field"
          :class="{ 'responsive-grid__mobile-field--full': field.fullWidth }"
        >
          <span class="responsive-grid__mobile-label">{{ field.label }}</span>
          <span class="responsive-grid__mobile-value">
            <slot
              v-if="$slots[`mobile-${field.id}`]"
              :name="`mobile-${field.id}`"
              :row="row"
              :field="field"
              :value="getCellValue(row, field)"
            />
            <slot
              v-else-if="$slots[`cell-${field.id}`]"
              :name="`cell-${field.id}`"
              :row="row"
              :column="field"
              :value="getCellValue(row, field)"
            />
            <span v-else>{{ formatCellValue(row, field) }}</span>
          </span>
        </div>

        <slot name="mobile-actions" :row="row" />
      </article>
    </div>

    <div v-else-if="!loading" class="responsive-grid__empty">
      {{ emptyText }}
    </div>

    <slot v-if="$slots.footer" name="footer" :data="data" :loading="loading" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

export type GridEditMode = 'inline' | 'batch' | 'popup' | 'popup-full-screen'

export type GridRow = Record<string, unknown>

export interface GridFilterOption {
  label: string
  value: string | number | boolean
}

export interface GridFilter {
  id: string
  label: string
  type?: 'text' | 'select'
  placeholder?: string
  value?: string | number | boolean | null
  options?: GridFilterOption[]
}

export interface GridColumn<T extends object = GridRow> {
  id: string
  label: string
  prop?: keyof T & string
  width?: string | number
  minWidth?: string | number
  align?: 'left' | 'center' | 'right'
  fixed?: 'left' | 'right'
  sortable?: boolean | 'custom'
}

export interface GridMobileField<T extends object = GridRow> {
  id: string
  label: string
  prop?: keyof T & string
  fullWidth?: boolean
}

interface Props {
  data: GridRow[]
  columns: GridColumn[]
  filters?: GridFilter[]
  mobileFields?: GridMobileField[]
  loading?: boolean
  emptyText?: string
  editMode?: GridEditMode
  showToolbar?: boolean
  rowKey?: string | ((row: GridRow) => string | number)
}

interface Emits {
  (event: 'filter-change', payload: { id: string; value: unknown }): void
}

interface Slots {
  toolbar?: () => unknown
  empty?: () => unknown
  footer?: (payload: { data: GridRow[]; loading: boolean }) => unknown
  mobileActions?: (payload: { row: GridRow }) => unknown
  [slotName: `header-${string}`]: (payload: { column: GridColumn; scope: unknown }) => unknown
  [slotName: `cell-${string}`]: (payload: { row: GridRow; column: GridColumn; value: unknown }) => unknown
  [slotName: `mobile-${string}`]: (payload: { row: GridRow; field?: GridMobileField; value?: unknown }) => unknown
}

const props = withDefaults(defineProps<Props>(), {
  filters: () => [],
  loading: false,
  emptyText: 'No records',
  editMode: 'inline',
  showToolbar: true,
  rowKey: 'id',
})

const emit = defineEmits<Emits>()
defineSlots<Slots>()

const filterValues = computed(() => {
  return Object.fromEntries(
    props.filters.map((filter) => [filter.id, filter.value ?? null])
  ) as Record<string, unknown>
})

const selectFilters = computed(() =>
  props.filters.filter((filter) => filter.type === 'select' || filter.options?.length)
)

const textFilters = computed(() =>
  props.filters.filter((filter) => filter.type === 'text' && !filter.options?.length)
)

const computedMobileFields = computed(() => {
  if (props.mobileFields?.length) {
    return props.mobileFields
  }

  return props.columns.map((column) => ({
    id: column.id,
    label: column.label,
    prop: column.prop,
    fullWidth: column.id === 'actions',
  }))
})

function resolveRowKey(row: GridRow): string | number {
  if (typeof props.rowKey === 'function') {
    return props.rowKey(row)
  }

  const value = row[props.rowKey]
  if (typeof value === 'string' || typeof value === 'number') {
    return value
  }

  return JSON.stringify(row)
}

function getCellValue(row: GridRow, column: GridColumn | GridMobileField): unknown {
  if (!column.prop) {
    return undefined
  }

  return row[column.prop]
}

function formatCellValue(row: GridRow, column: GridColumn | GridMobileField): string {
  const value = getCellValue(row, column)

  if (value === null || value === undefined || value === '') {
    return '—'
  }

  if (value instanceof Date) {
    return value.toLocaleString()
  }

  return String(value)
}

function updateFilter(id: string, value: unknown): void {
  emit('filter-change', { id, value })
}
</script>

<style scoped>
.responsive-grid {
  width: 100%;
  min-width: 0;
}

.responsive-grid__toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
  min-width: 0;
}

.responsive-grid__filters {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  min-width: 0;
}

.responsive-grid__filter {
  width: 220px;
  max-width: 100%;
  min-width: 0;
}

.responsive-grid__table,
.responsive-grid__desktop-table {
  width: 100%;
  min-width: 0;
}

.responsive-grid__mobile-cards {
  display: none;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}

.responsive-grid__mobile-card {
  display: grid;
  gap: 12px;
  padding: 14px;
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  background: var(--el-fill-color-light);
  min-width: 0;
}

.responsive-grid__mobile-field {
  display: grid;
  gap: 4px;
  min-width: 0;
}

.responsive-grid__mobile-label {
  color: var(--el-text-color-secondary);
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.responsive-grid__mobile-value {
  display: block;
  color: var(--el-text-color-primary);
  overflow-wrap: anywhere;
  word-break: break-word;
}

.responsive-grid__empty {
  padding: 24px;
  color: var(--el-text-color-placeholder);
  text-align: center;
}

@media (max-width: 768px) {
  .responsive-grid__toolbar {
    display: flex;
    flex-direction: column;
    align-items: stretch;
  }

  .responsive-grid__filters {
    width: 100%;
  }

  .responsive-grid__filter {
    flex: 1 1 100%;
    width: 100%;
  }

  .responsive-grid__table,
  .responsive-grid__desktop-table {
    display: none;
    overflow-x: hidden;
  }

  .responsive-grid__mobile-cards {
    display: flex;
  }

  .responsive-grid__empty {
    padding: 16px;
  }
}
</style>
