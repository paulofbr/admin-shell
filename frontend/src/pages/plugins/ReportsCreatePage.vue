<template>
  <div class="plugin-page">
    <h2>Create Report</h2>
    <p class="plugin-page__subtitle">Quick action from header toolbar (contributed by <strong>ReportingPlugin</strong> via <code>IHeaderActionPlugin</code>).</p>

    <el-form label-width="140px" style="max-width: 500px;">
      <el-form-item label="Report Type">
        <el-select v-model="reportType" placeholder="Select report type" style="width: 100%;">
          <el-option v-for="r in reports" :key="r.id" :label="r.name" :value="r.id" />
        </el-select>
      </el-form-item>
      <el-form-item label="Format">
        <el-select v-model="format" placeholder="Select format" style="width: 100%;">
          <el-option label="JSON" value="json" />
          <el-option label="CSV" value="csv" />
          <el-option label="PDF" value="pdf" />
        </el-select>
      </el-form-item>
      <el-form-item label="Date Range">
        <el-date-picker v-model="dateRange" type="daterange" range-separator="to" style="width: 100%;" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="generateReport">Generate Report</el-button>
      </el-form-item>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useExtensionStore } from '@/stores/extensionStore'
import { ElMessage } from 'element-plus'

const extensionStore = useExtensionStore()
const reports = computed(() => extensionStore.reports)
const reportType = ref('')
const format = ref('json')
const dateRange = ref(null)

function generateReport() {
  ElMessage.success('Report generation triggered')
}
</script>

<style scoped>
.plugin-page { padding: 24px; }
.plugin-page__subtitle { color: var(--el-text-color-secondary); margin-bottom: 24px; }
</style>