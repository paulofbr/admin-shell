<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import {
  ElButton,
  ElCard,
  ElCol,
  ElRow,
  ElSkeleton,
  ElStatistic,
  ElTable,
  ElTableColumn,
  ElTag,
} from 'element-plus'
import type { OrderDto, OrderSummaryDto } from '../types'
import { formatDate, formatMoney, usePluginServices } from '../types'
import { getOrderSummary, getOrders } from '../api/orderCreationApi'

const services = usePluginServices()

const summary = ref<OrderSummaryDto | null>(null)
const orders = ref<OrderDto[]>([])
const loading = ref(false)

let unsubscribeCreated: (() => void) | undefined

onMounted(() => {
  unsubscribeCreated = services.eventBus.subscribe('orders:created', load)
  load()
})

onUnmounted(() => {
  unsubscribeCreated?.()
})

async function load() {
  loading.value = true

  try {
    const [summaryValue, ordersValue] = await Promise.all([
      getOrderSummary(),
      getOrders(1, 10),
    ])

    summary.value = summaryValue
    orders.value = ordersValue.data
  } catch {
    services.ui.showNotification('Falha ao carregar resumo de encomendas', 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="order-summary-page">
    <div class="order-summary-page__hero">
      <div>
        <p class="order-summary-page__eyebrow">Order Creation Plugin</p>
        <h1>Encomendas</h1>
        <p>Resumo operacional e últimas encomendas criadas pelo frontend Vue SFC.</p>
      </div>
      <el-button type="primary" @click="services.navigation.navigate('/orders/create')">
        Nova encomenda
      </el-button>
    </div>

    <div v-if="loading">
      <el-skeleton animated :rows="8" />
    </div>

    <template v-else>
      <el-row :gutter="16" class="order-summary-page__stats">
        <el-col :xs="24" :md="8">
          <el-statistic title="Encomendas abertas" :value="summary?.openOrders || 0" />
        </el-col>
        <el-col :xs="24" :md="8">
          <el-statistic title="Valor em aberto" :value="summary?.openValue || 0" :precision="2" />
        </el-col>
        <el-col :xs="24" :md="8">
          <el-statistic
            title="Próxima referência"
            :value="0"
            :formatter="() => summary?.nextOrderNumber || '-'"
          />
        </el-col>
        <el-col :xs="24" :md="8">
          <el-statistic title="Faturado hoje" :value="summary?.todayRevenue || 0" :precision="2" />
        </el-col>
        <el-col :xs="24" :md="8">
          <el-statistic title="Produtos com stock baixo" :value="summary?.lowStockProducts || 0" />
        </el-col>
        <el-col :xs="24" :md="8">
          <el-button style="width: 100%;" @click="load">
            Atualizar
          </el-button>
        </el-col>
      </el-row>

      <el-card shadow="never">
        <template #header>
          <div class="order-summary-page__table-header">
            <strong>Últimas encomendas</strong>
            <el-tag type="info">{{ orders.length }} registos</el-tag>
          </div>
        </template>

        <el-table :data="orders" style="width: 100%;">
          <el-table-column prop="orderNumber" label="Referência" min-width="150" />
          <el-table-column prop="customerName" label="Cliente" min-width="180" />
          <el-table-column label="Linhas" width="100">
            <template #default="scope">
              {{ scope.row.lines?.length || 0 }}
            </template>
          </el-table-column>
          <el-table-column prop="status" label="Estado" width="120">
            <template #default="scope">
              <el-tag :type="scope.row.status === 'Aberta' ? 'success' : 'info'">
                {{ scope.row.status }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Entrega" width="150">
            <template #default="scope">
              {{ formatDate(scope.row.requestedDeliveryDate) }}
            </template>
          </el-table-column>
          <el-table-column label="Total" width="150" align="right">
            <template #default="scope">
              <strong>{{ formatMoney(scope.row.total, scope.row.currency || 'EUR') }}</strong>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </template>
  </div>
</template>

<style scoped>
.order-summary-page {
  display: grid;
  gap: 18px;
}

.order-summary-page__hero {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 24px;
  border-radius: 18px;
  background:
    radial-gradient(circle at top left, rgba(64, 158, 255, 0.18), transparent 34%),
    var(--el-fill-color-light);
}

.order-summary-page__hero > div {
  min-width: 0;
}

.order-summary-page__eyebrow {
  margin: 0 0 6px;
  color: var(--el-color-primary);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.order-summary-page__hero h1 {
  margin: 0;
  color: var(--el-text-color-primary);
  font-size: clamp(24px, 4vw, 34px);
}

.order-summary-page__hero p:not(.order-summary-page__eyebrow) {
  max-width: 760px;
  margin: 8px 0 0;
  color: var(--el-text-color-secondary);
}

.order-summary-page__stats {
  width: 100%;
}

.order-summary-page__table-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

@media (max-width: 768px) {
  .order-summary-page__hero {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
