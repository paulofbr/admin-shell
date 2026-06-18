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
        v-loading="displayLoading"
        :data="displayData"
        :row-key="resolveRowKey"
        stripe
        style="width: 100%"
        class="responsive-grid__desktop-table"
      >
        <el-table-column
          v-for="column in gridColumns"
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
              :editing="isInlineEditing(cellScope.row as GridRow)"
              :save="() => saveInlineEditor(cellScope.row as GridRow)"
              :cancel="() => cancelInlineEditor(cellScope.row as GridRow)"
            >
              <span
                v-if="!isInlineEditing(cellScope.row as GridRow)"
                class="responsive-grid__cell-value"
              >
                {{ formatCellValue(cellScope.row as GridRow, column) }}
              </span>
              <slot
                v-else
                :name="`inline-editor-${column.id}`"
                :row="cellScope.row as GridRow"
                :column="column"
                :value="getCellValue(cellScope.row as GridRow, column)"
                :save="() => saveInlineEditor(cellScope.row as GridRow)"
                :cancel="() => cancelInlineEditor(cellScope.row as GridRow)"
              />
            </slot>
          </template>
        </el-table-column>

        <el-table-column
          v-if="shouldShowActionColumn && !hasActionsColumn"
          key="actions"
          label="Actions"
          width="160"
          fixed="right"
        >
          <template #default="{ row }">
            <div class="responsive-grid__actions">
              <slot
                v-if="$slots['cell-actions']"
                name="cell-actions"
                :row="row as GridRow"
              />
              <template v-else>
                <el-button
                  v-if="editMode === 'popup' || editMode === 'fullpage'"
                  link
                  type="primary"
                  @click="openEditor(row as GridRow)"
                >
                  {{ editButtonLabel }}
                </el-button>
                <el-button
                  v-else
                  link
                  type="primary"
                  @click="openInlineEditor(row as GridRow)"
                >
                  {{ editButtonLabel }}
                </el-button>

                <el-button
                  v-if="deletable"
                  link
                  type="danger"
                  @click="handleDelete(row as GridRow)"
                >
                  {{ deleteButtonLabel }}
                </el-button>
              </template>
            </div>
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

    <div v-if="displayData.length > 0" class="responsive-grid__mobile-cards" data-responsive-grid-mobile-cards>
      <article
        v-for="row in displayData"
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

        <div v-if="shouldShowActionColumn && !$slots['mobile-actions']" class="responsive-grid__mobile-actions">
          <el-button
            v-if="editMode === 'popup' || editMode === 'fullpage'"
            link
            type="primary"
            @click="openEditor(row)"
          >
            {{ editButtonLabel }}
          </el-button>
          <el-button
            v-else
            link
            type="primary"
            @click="openInlineEditor(row)"
          >
            {{ editButtonLabel }}
          </el-button>

          <el-button
            v-if="deletable"
            link
            type="danger"
            @click="handleDelete(row)"
          >
            {{ deleteButtonLabel }}
          </el-button>
        </div>

        <slot name="mobile-actions" :row="row" />
      </article>
    </div>

    <div v-else-if="!loading" class="responsive-grid__empty">
      {{ emptyText }}
    </div>

    <div v-if="$slots.footer || $slots.default || shouldShowDefaultFooter" class="responsive-grid__footer">
      <slot
        v-if="$slots.footer"
        name="footer"
        :data="displayData"
        :loading="displayLoading"
      />
      <slot
        v-else-if="$slots.default"
        :data="displayData"
        :loading="displayLoading"
      />
      <template v-else>
        <div v-if="shouldShowPagination" class="responsive-grid__pagination">
          <el-pagination
            v-model:current-page="currentPageModel"
            :page-size="pageSize"
            :total="total"
            :layout="pageLayout"
          />
        </div>

        <div v-if="displayLoading" class="responsive-grid__loading-footer">
          <el-skeleton :rows="6" animated />
        </div>
      </template>
    </div>

    <el-dialog
      v-if="editMode === 'popup'"
      v-model="editorOpen"
      :width="'90%'"
      :close-on-click-modal="false"
      center
    >
      <component
        :is="editorComponent"
        v-if="editorComponent"
        v-bind="editorComponentProps"
        @saved="handleEditorSaved"
      />
    </el-dialog>

    <div v-if="editMode === 'fullpage' && editorOpen" class="responsive-grid__fullpage-editor">
      <div class="responsive-grid__fullpage-backdrop" @click="closeEditor" />
      <aside class="responsive-grid__fullpage-panel">
        <div class="responsive-grid__fullpage-header">
          <div>
            <p v-if="editorSubtitle" class="responsive-grid__fullpage-subtitle">
              {{ editorSubtitle }}
            </p>
            <h2 class="responsive-grid__fullpage-title">
              {{ editorSubtitle ?? 'Edit' }}
            </h2>
          </div>
          <el-button type="default" @click="closeEditor">
            Close
          </el-button>
        </div>

        <div class="responsive-grid__fullpage-content">
          <component
            :is="editorComponent"
            v-if="editorComponent"
            v-bind="editorComponentProps"
            @saved="handleEditorSaved"
          />
        </div>
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, useSlots, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { Component } from 'vue'

export type GridEditMode = 'none' | 'inline' | 'batch' | 'popup' | 'fullpage'

export type GridRow = Record<string, unknown>

export type EditorModelLoader = (key: unknown, row: GridRow) => Promise<unknown> | unknown

export interface GridLoadQuery {
  currentPage: number
  pageSize: number
  skip: number
  filters: Record<string, unknown>
}

export interface GridLoadResult<T extends GridRow = GridRow> {
  data: T[]
  total?: number
}

export type GridDataLoader<T extends GridRow = GridRow> = (
  query: GridLoadQuery
) => Promise<GridLoadResult<T>> | GridLoadResult<T>

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
  data?: GridRow[]
  columns: GridColumn[]
  filters?: GridFilter[]
  mobileFields?: GridMobileField[]
  loading?: boolean
  emptyText?: string
  editMode?: GridEditMode
  editable?: boolean
  deletable?: boolean
  editButtonLabel?: string
  deleteButtonLabel?: string
  deleteConfirmTitle?: string
  deleteConfirmMessage?: string | ((row: GridRow) => string)
  deleteSuccessMessage?: string | ((row: GridRow) => string)
  deleteErrorMessage?: string | ((error: unknown, row: GridRow) => string)
  showToolbar?: boolean
  rowKey?: string | ((row: GridRow) => string | number)
  total?: number
  currentPage?: number
  pageSize?: number
  pageLayout?: string
  editorSubtitle?: string
  editorComponent?: Component
  getEditorModel?: EditorModelLoader
  editorKeyProp?: string
  saveToProperty?: string
  loadData?: GridDataLoader
  onDelete?: (row: GridRow) => Promise<void> | void
}

interface Emits {
  (event: 'filter-change', payload: { id: string; value: unknown }): void
  (event: 'page-change', page: number): void
  (event: 'editor-open', row?: GridRow): void
  (event: 'editor-close', row?: GridRow): void
  (event: 'editor-saved', value: unknown): void
  (event: 'edit-request', row: GridRow): void
  (event: 'delete-request', row: GridRow): void
  (event: 'delete-success', row: GridRow): void
  (event: 'inline-edit', row: GridRow): void
  (event: 'inline-cancel', row: GridRow): void
  (event: 'inline-save', row: GridRow): void
  (event: 'delete-error', error: unknown, row: GridRow): void
}

interface Slots {
  default?: (payload: { data: GridRow[]; loading: boolean }) => unknown
  toolbar?: () => unknown
  empty?: () => unknown
  footer?: (payload: { data: GridRow[]; loading: boolean }) => unknown
  mobileActions?: (payload: { row: GridRow }) => unknown
  [slotName: `header-${string}`]: (payload: { column: GridColumn; scope: unknown }) => unknown
  [slotName: `cell-${string}`]: (payload: {
    row: GridRow
    column: GridColumn
    value: unknown
    editing: boolean
    save: () => void
    cancel: () => void
  }) => unknown
  [slotName: `mobile-${string}`]: (payload: { row: GridRow; field?: GridMobileField; value?: unknown }) => unknown
  [slotName: `inline-editor-${string}`]: (payload: {
    row: GridRow
    column: GridColumn
    value: unknown
    save: () => void
    cancel: () => void
  }) => unknown
}

const props = withDefaults(defineProps<Props>(), {
  filters: () => [],
  loading: false,
  emptyText: 'No records',
  editMode: 'none',
  editable: false,
  deletable: false,
  editButtonLabel: 'Edit',
  deleteButtonLabel: 'Delete',
  deleteConfirmTitle: 'Delete item',
  showToolbar: true,
  rowKey: 'id',
  pageSize: 10,
  pageLayout: 'prev, pager, next',
  editorKeyProp: 'id',
})

const emit = defineEmits<Emits>()
const slots = useSlots()
defineSlots<Slots>()

const loadedData = ref<GridRow[]>([])
const loadedTotal = ref<number | undefined>(undefined)
const internalLoading = ref(false)

const displayData = computed(() => props.loadData ? loadedData.value : props.data ?? [])
const displayLoading = computed(() => props.loadData ? internalLoading.value : props.loading)
const displayTotal = computed(() => props.loadData ? loadedTotal.value : props.total)

const hasActionsColumn = computed(
  () => props.columns.some((column) => column.id === 'actions') || !!slots['cell-actions'],
)

const gridColumns = computed(() => {
  if (!shouldShowActionColumn.value || hasActionsColumn.value) {
    return props.columns
  }

  return [
    ...props.columns,
    {
      id: 'actions',
      label: 'Actions',
      width: 160,
      fixed: 'right' as const,
      sortable: false,
    } as GridColumn,
  ]
})

const shouldShowActionColumn = computed(
  () => props.editable && props.editMode !== 'none' && props.editMode !== 'batch',
)

const filterValues = computed(() => {
  return Object.fromEntries(
    props.filters.map((filter) => [filter.id, filter.value ?? null])
  ) as Record<string, unknown>
})

const fallbackCurrentPage = ref(1)

const currentPageModel = computed({
  get: () => props.loadData ? fallbackCurrentPage.value : props.currentPage ?? fallbackCurrentPage.value,
  set: (page: number) => {
    fallbackCurrentPage.value = page
    emit('page-change', page)

    if (props.loadData) {
      void loadDataForPage(page, filterValues.value)
    }
  },
})

const shouldShowPagination = computed(() => {
  const totalValue = displayTotal.value
  return totalValue !== undefined && totalValue > props.pageSize
})

const shouldShowDefaultFooter = computed(
  () => shouldShowPagination.value || displayLoading.value,
)

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

const editorRow = ref<GridRow | null>(null)
const editorOpen = ref(false)
const editorModel = ref<unknown>(null)
const inlineEditingRowKey = ref<string | number | null>(null)

async function loadDataForPage(page: number, filters: Record<string, unknown>): Promise<void> {
  if (!props.loadData) {
    return
  }

  const skip = (page - 1) * props.pageSize

  internalLoading.value = true

  try {
    const result = await props.loadData({
      currentPage: page,
      pageSize: props.pageSize,
      skip,
      filters,
    })

    loadedData.value = (result.data ?? []) as GridRow[]
    loadedTotal.value = result.total
  } finally {
    internalLoading.value = false
  }
}

async function refresh(): Promise<void> {
  if (!props.loadData) return
  await loadDataForPage(currentPageModel.value, filterValues.value)
}

async function openEditor(row: GridRow | null): Promise<void> {
  editorRow.value = row
  editorOpen.value = true

  if (row) {
    const key = row[props.editorKeyProp]
    editorModel.value = props.getEditorModel
      ? await props.getEditorModel(key, row)
      : row
  } else {
    editorModel.value = null
  }

  emit('editor-open', row ?? undefined)
}

function closeEditor(): void {
  editorOpen.value = false
  editorRow.value = null
  editorModel.value = null
  emit('editor-close')
}

const editorComponentProps = computed(() => ({
  modelValue: editorModel.value,
  row: editorRow.value,
  keyValue: editorRow.value?.[props.editorKeyProp],
  close: closeEditor,
}))

function resolveEditorSavedPayload(value: unknown): unknown {
  if (!props.saveToProperty) return value

  const gridPayload = displayData.value

  if (value && typeof value === 'object' && !Array.isArray(value)) {
    return {
      ...(value as Record<string, unknown>),
      [props.saveToProperty]: gridPayload,
    }
  }

  return {
    entity: value,
    [props.saveToProperty]: gridPayload,
  }
}

function handleEditorSaved(value: unknown): void {
  const payload = resolveEditorSavedPayload(value)
  emit('editor-saved', payload)

  if (props.loadData) {
    void refresh()
  }
}

function resolveDeleteSuccessMessage(row: GridRow): string {
  const message = props.deleteSuccessMessage

  return typeof message === 'function' ? message(row) : message ?? 'Deleted successfully'
}

function resolveDeleteErrorMessage(error: unknown, row: GridRow): string {
  const message = props.deleteErrorMessage

  if (typeof message === 'function') {
    return message(error, row)
  }

  if (message) {
    return message
  }

  return error instanceof Error ? error.message : 'Failed to delete item'
}

async function handleDelete(row: GridRow): Promise<void> {
  const message =
    typeof props.deleteConfirmMessage === 'function'
      ? props.deleteConfirmMessage(row)
      : 'Are you sure you want to delete this item?'

  let confirmed = false

  try {
    confirmed = await ElMessageBox.confirm(message, props.deleteConfirmTitle, {
      confirmButtonText: props.deleteButtonLabel,
      cancelButtonText: 'Cancel',
      type: 'warning',
    })
  } catch (error) {
    if (error === 'cancel' || error === 'close') {
      return
    }

    throw error
  }

  if (!confirmed) return

  try {
    if (props.onDelete) {
      await props.onDelete(row)
    }

    emit('delete-success', row)
    ElMessage.success(resolveDeleteSuccessMessage(row))
    emit('delete-request', row)

    if (props.loadData) {
      await refresh()
    }
  } catch (error) {
    emit('delete-error', error, row)
    ElMessage.error(resolveDeleteErrorMessage(error, row))
  }
}

function openInlineEditor(row: GridRow): void {
  inlineEditingRowKey.value = resolveRowKey(row)
  emit('inline-edit', row)
}

function cancelInlineEditor(row: GridRow): void {
  inlineEditingRowKey.value = null
  emit('inline-cancel', row)
}

function saveInlineEditor(row: GridRow): void {
  inlineEditingRowKey.value = null
  emit('inline-save', row)
}

function isInlineEditing(row: GridRow): boolean {
  return props.editMode === 'inline' && inlineEditingRowKey.value === resolveRowKey(row)
}

onMounted(() => {
  if (props.loadData) {
    void refresh()
  }
})

watch(
  () => [props.loadData, props.pageSize],
  () => {
    if (props.loadData) {
      void loadDataForPage(fallbackCurrentPage.value, filterValues.value)
    }
  },
)

const isLoading = computed(() => displayLoading.value)

defineExpose({
  openEditor,
  closeEditor,
  openInlineEditor,
  cancelInlineEditor,
  saveInlineEditor,
  refresh,
  isLoading,
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
  const nextFilters = {
    ...filterValues.value,
    [id]: value,
  }

  emit('filter-change', { id, value })

  if (props.loadData) {
    fallbackCurrentPage.value = 1
    void loadDataForPage(1, nextFilters)
  }
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

.responsive-grid__actions,
.responsive-grid__mobile-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  justify-content: flex-end;
}

.responsive-grid__mobile-actions {
  justify-content: flex-start;
}

.responsive-grid__empty {
  padding: 24px;
  color: var(--el-text-color-placeholder);
  text-align: center;
}

.responsive-grid__footer {
  margin-top: 16px;
}

.responsive-grid__pagination {
  display: flex;
  justify-content: center;
}

.responsive-grid__loading-footer {
  padding: 60px 0;
  text-align: center;
}

.responsive-grid__fullpage-editor {
  position: fixed;
  inset: 0;
  z-index: 3000;
  display: flex;
  margin-left: 240px;
}

.responsive-grid__fullpage-backdrop {
  position: absolute;
  inset: 0;
  background: rgb(0 0 0 / 24%);
}

.responsive-grid__fullpage-panel {
  position: relative;
  display: grid;
  grid-template-rows: auto 1fr;
  width: min(720px, calc(100vw - 240px));
  min-width: 0;
  height: 100%;
  padding: 24px;
  background: var(--el-bg-color);
  box-shadow: var(--el-box-shadow-light);
  overflow-y: auto;
}

.responsive-grid__fullpage-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 18px;
  border-bottom: 1px solid var(--el-border-color-lighter);
}

.responsive-grid__fullpage-title,
.responsive-grid__fullpage-subtitle {
  margin: 0;
}

.responsive-grid__fullpage-title {
  color: var(--el-text-color-primary);
  font-size: 20px;
  font-weight: 700;
}

.responsive-grid__fullpage-subtitle {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.responsive-grid__fullpage-content {
  min-width: 0;
  padding-top: 20px;
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

  .responsive-grid__loading-footer {
    padding: 40px 0;
  }

  .responsive-grid__fullpage-editor {
    margin-left: 0;
  }

  .responsive-grid__fullpage-panel {
    width: 100%;
  }
}
</style>
