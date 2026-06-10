<template>
  <div class="plugin-page">
    <h2>Reports</h2>
    <p class="plugin-page__subtitle">Contributed by <strong>ReportingPlugin</strong> via <code>IMenuPlugin</code>.</p>

    <el-row :gutter="16" style="margin-bottom: 20px;">
      <el-col v-for="report in reports" :key="report.id" :span="6">
        <el-card shadow="hover" class="report-card" @click="viewReport(report)">
          <el-icon :size="24" class="report-card__icon"><Document /></el-icon>
          <h3 class="report-card__title">{{ report.name }}</h3>
          <p class="report-card__desc">{{ report.description }}</p>
          <div class="report-card__formats">
            <el-tag v-for="fmt in report.supportedFormats" :key="fmt" size="small">{{ fmt }}</el-tag>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-empty v-if="reports.length === 0" description="No reports loaded" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useExtensionStore } from '@/stores/extensionStore'
import { ElMessage } from 'element-plus'
import { Document } from '@element-plus/icons-vue'

const extensionStore = useExtensionStore()
const reports = computed(() => extensionStore.reports)

function viewReport(report: any) {
  ElMessage.info(`Report: ${report.name} — endpoint: ${report.reportEndpoint}`)
}
</script>

<style scoped>
.plugin-page { padding: 24px; }
.plugin-page__subtitle { color: var(--el-text-color-secondary); margin-bottom: 24px; }
.report-card { cursor: pointer; transition: transform 0.15s; }
.report-card:hover { transform: translateY(-2px); }
.report-card__icon { color: var(--el-color-primary); margin-bottom: 8px; }
.report-card__title { font-size: 14px; margin: 0 0 4px; }
.report-card__desc { font-size: 12px; color: var(--el-text-color-secondary); margin-bottom: 8px; }
.report-card__formats { display: flex; gap: 4px; flex-wrap: wrap; }
</style>