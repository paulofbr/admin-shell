<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElButton, ElTable, ElTableColumn, ElTag } from 'element-plus'
import EntityEditor from '@admin-shell/ui/EntityEditor.vue'
import type { CustomerDto, ProductDto } from '../types'
import { formatMoney, usePluginServices } from '../types'
import { createOrderApi, getOrderCustomers, getOrderProducts } from '../api/orderCreationApi'

interface OrderLine {
  productId: string
  quantity: number
  discountPercent: number
}

const services = usePluginServices()

const customers = ref<CustomerDto[]>([])
const products = ref<ProductDto[]>([])
const loadingCatalog = ref(false)
const submitting = ref(false)

const form = reactive({
  customerId: '',
  poNumber: '',
  requestedDeliveryDate: '',
  paymentMethod: 'Fatura',
  notes: '',
  shippingAddress: {
    street: '',
    city: '',
    postalCode: '',
    country: 'Portugal',
  },
})

const lines = ref<OrderLine[]>([
  { productId: '', quantity: 1, discountPercent: 0 },
])

const selectedCustomer = computed(() =>
  customers.value.find((customer) => String(customer.id) === String(form.customerId)),
)

const orderLines = computed(() =>
  lines.value.map((line) => {
    const product = products.value.find((item) => String(item.id) === String(line.productId))
    const discount = Math.max(0, Math.min(100, Number(line.discountPercent || 0)))
    const quantity = Math.max(1, Number(line.quantity || 1))
    const unitPrice = product ? Number(product.unitPrice || 0) * (1 - discount / 100) : 0

    return {
      ...line,
      product,
      quantity,
      discount,
      unitPrice,
      lineTotal: unitPrice * quantity,
    }
  }),
)

const subtotal = computed(() => orderLines.value.reduce((sum, line) => sum + line.lineTotal, 0))
const vat = computed(() => subtotal.value * 0.23)
const shippingCost = computed(() => subtotal.value >= 250 ? 0 : 7.5)
const total = computed(() => subtotal.value + vat.value + shippingCost.value)

onMounted(loadCatalog)

async function loadCatalog() {
  loadingCatalog.value = true

  try {
    const [customerValue, productValue] = await Promise.all([
      getOrderCustomers(),
      getOrderProducts(),
    ])

    customers.value = customerValue
    products.value = productValue
    form.customerId = customers.value[0]?.id ?? ''

    if (products.value[0]) {
      lines.value[0].productId = products.value[0].id
    }
  } finally {
    loadingCatalog.value = false
  }
}

function addLine() {
  lines.value.push({ productId: products.value[0]?.id ?? '', quantity: 1, discountPercent: 0 })
}

function removeLine(index: number) {
  if (lines.value.length === 1) {
    services.ui.showNotification('A encomenda precisa de pelo menos uma linha', 'warning')
    return
  }

  lines.value.splice(index, 1)
}

async function submitOrder() {
  if (!form.customerId) {
    services.ui.showNotification('Selecione um cliente', 'warning')
    return
  }

  if (lines.value.some((line) => !line.productId || Number(line.quantity || 0) <= 0)) {
    services.ui.showNotification('Preencha produto e quantidade em todas as linhas', 'warning')
    return
  }

  submitting.value = true

  try {
    const payload = {
      customerId: form.customerId,
      poNumber: form.poNumber || null,
      requestedDeliveryDate: form.requestedDeliveryDate || null,
      paymentMethod: form.paymentMethod,
      notes: form.notes || null,
      shippingAddress: form.shippingAddress,
      lines: lines.value.map((line) => ({
        productId: line.productId,
        quantity: Number(line.quantity),
        discountPercent: Number(line.discountPercent || 0),
      })),
    }

    const createdOrder = await createOrderApi(payload)

    services.eventBus.publish('orders:created', createdOrder)
    services.ui.showNotification(`Encomenda ${createdOrder.orderNumber} criada com sucesso`, 'success')
    services.navigation.navigate('/orders/summary')
  } catch {
    services.ui.showNotification('Falha ao criar encomenda', 'error')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <EntityEditor
    title="Criar encomenda"
    subtitle="Fluxo completo: cliente, produto, descontos, IVA, expedição e submissão para API do plugin."
    :save-label="submitting ? 'A criar...' : 'Criar encomenda'"
    :save-loading="submitting"
    :on-save="submitOrder"
    :on-cancel="() => services.navigation.navigate('/orders')"
  >
    <template #toolbar-actions>
      <el-tag type="success">Frontend Vue SFC</el-tag>
    </template>

    <div v-if="loadingCatalog" class="order-create-page__card">
      Carregar catálogo...
    </div>

    <div v-else class="order-create-page__card">
      <form class="order-create-page__form" @submit.prevent="submitOrder">
        <section>
          <h2>Cliente</h2>
          <label class="order-create-page__field">
            <span>Cliente</span>
            <select v-model="form.customerId">
              <option
                v-for="customer in customers"
                :key="customer.id"
                :value="customer.id"
              >
                {{ customer.name }}
              </option>
            </select>
          </label>

          <div class="order-create-page__row">
            <label class="order-create-page__field">
              <span>PO / Referência</span>
              <input v-model="form.poNumber" placeholder="Opcional">
            </label>
            <label class="order-create-page__field">
              <span>Entrega prevista</span>
              <input v-model="form.requestedDeliveryDate" type="date">
            </label>
          </div>

          <label class="order-create-page__field">
            <span>Método de pagamento</span>
            <select v-model="form.paymentMethod">
              <option value="Fatura">Fatura</option>
              <option value="Transferência">Transferência</option>
              <option value="Cartão">Cartão</option>
              <option value="MB Way">MB Way</option>
            </select>
          </label>
        </section>

        <section>
          <h2>Linhas</h2>
          <div class="order-create-page__table-wrapper">
            <el-table :data="orderLines" style="width: 100%;">
              <el-table-column label="Produto" min-width="180">
                <template #default="scope">
                  <select v-model="scope.row.productId">
                    <option
                      v-for="product in products"
                      :key="product.id"
                      :value="product.id"
                    >
                      {{ product.sku }} — {{ product.name }}
                    </option>
                  </select>
                </template>
              </el-table-column>

              <el-table-column label="Quantidade" width="120">
                <template #default="scope">
                  <input
                    v-model.number="scope.row.quantity"
                    type="number"
                    min="1"
                  >
                </template>
              </el-table-column>

              <el-table-column label="Desconto %" width="120">
                <template #default="scope">
                  <input
                    v-model.number="scope.row.discountPercent"
                    type="number"
                    min="0"
                    max="100"
                  >
                </template>
              </el-table-column>

              <el-table-column label="Preço unitário" width="140">
                <template #default="scope">
                  {{ formatMoney(scope.row.unitPrice, 'EUR') }}
                </template>
              </el-table-column>

              <el-table-column label="Total" width="140" align="right">
                <template #default="scope">
                  <strong>{{ formatMoney(scope.row.lineTotal, 'EUR') }}</strong>
                </template>
              </el-table-column>

              <el-table-column label="" width="100">
                <template #default="scope">
                  <button
                    type="button"
                    class="order-create-page__link-button"
                    @click="removeLine(scope.$index)"
                  >
                    Remover
                  </button>
                </template>
              </el-table-column>
            </el-table>
          </div>

          <button
            type="button"
            class="order-create-page__secondary-button"
            @click="addLine"
          >
            Adicionar linha
          </button>
        </section>

        <section>
          <h2>Expedição</h2>
          <div class="order-create-page__row">
            <label class="order-create-page__field">
              <span>Morada</span>
              <input v-model="form.shippingAddress.street" placeholder="Rua, número">
            </label>
            <label class="order-create-page__field">
              <span>Cidade</span>
              <input v-model="form.shippingAddress.city" :placeholder="selectedCustomer?.city || 'Cidade'">
            </label>
          </div>

          <div class="order-create-page__row">
            <label class="order-create-page__field">
              <span>Código postal</span>
              <input v-model="form.shippingAddress.postalCode" placeholder="0000-000">
            </label>
            <label class="order-create-page__field">
              <span>País</span>
              <input v-model="form.shippingAddress.country">
            </label>
          </div>

          <label class="order-create-page__field order-create-page__field--wide">
            <span>Notas internas</span>
            <textarea
              v-model="form.notes"
              rows="3"
              placeholder="Informação para preparação da encomenda"
            />
          </label>
        </section>

        <section>
          <h2>Resumo financeiro</h2>
          <div class="order-create-page__stats">
            <div>
              <span>Subtotal</span>
              <strong>{{ formatMoney(subtotal, 'EUR') }}</strong>
            </div>
            <div>
              <span>IVA 23%</span>
              <strong>{{ formatMoney(vat, 'EUR') }}</strong>
            </div>
            <div>
              <span>Portes</span>
              <strong>{{ formatMoney(shippingCost, 'EUR') }}</strong>
            </div>
            <div>
              <span>Total</span>
              <strong>{{ formatMoney(total, 'EUR') }}</strong>
            </div>
          </div>
        </section>
      </form>
    </div>
  </EntityEditor>
</template>

<style scoped>
.order-create-page__card {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 14px;
  padding: 18px;
  background: var(--el-bg-color);
}

.order-create-page__form,
.order-create-page__stats {
  display: grid;
  gap: 18px;
}

.order-create-page__row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.order-create-page__field {
  display: grid;
  gap: 6px;
  color: var(--el-text-color-regular);
  font-size: 13px;
}

.order-create-page__field--wide {
  grid-column: 1 / -1;
}

.order-create-page__field select,
.order-create-page__field input,
.order-create-page__field textarea {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  padding: 8px 10px;
  background: var(--el-fill-color-blank);
  color: var(--el-text-color-primary);
  outline: none;
}

.order-create-page__field select:focus,
.order-create-page__field input:focus,
.order-create-page__field textarea:focus {
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 2px var(--el-color-primary-light-9);
}

.order-create-page__table-wrapper {
  width: 100%;
  overflow-x: auto;
}

.order-create-page__table-wrapper :deep(.el-table__inner-wrapper) {
  min-width: 760px;
}

.order-create-page__link-button {
  border: 0;
  background: transparent;
  color: var(--el-color-primary);
  cursor: pointer;
  font-weight: 600;
}

.order-create-page__secondary-button {
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  padding: 8px 12px;
  background: var(--el-fill-color-blank);
  color: var(--el-text-color-primary);
  cursor: pointer;
}

.order-create-page__secondary-button:hover {
  border-color: var(--el-color-primary);
}

.order-create-page__stats {
  grid-template-columns: repeat(4, minmax(0, 1fr));
}

.order-create-page__stats > div {
  display: grid;
  gap: 4px;
  border-radius: 12px;
  padding: 12px;
  background: var(--el-fill-color-light);
}

.order-create-page__stats span {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.order-create-page__stats strong {
  color: var(--el-color-primary);
  font-size: 18px;
}

@media (max-width: 768px) {
  .order-create-page__row,
  .order-create-page__stats {
    grid-template-columns: 1fr;
  }
}
</style>
