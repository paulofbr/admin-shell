<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import type { CustomerDto, ProductDto, WidgetDescriptor } from '../types'
import { formatMoney, usePluginServices } from '../types'
import { createOrderApi, getOrderCustomers, getOrderProducts } from '../api/orderCreationApi'

const props = defineProps<{
  widget?: WidgetDescriptor
  pluginId?: string
}>()

const services = usePluginServices(props.pluginId)

const customers = ref<CustomerDto[]>([])
const products = ref<ProductDto[]>([])
const selectedCustomerId = ref('')
const selectedProductId = ref('')
const quantity = ref(1)
const discountPercent = ref(0)
const loading = ref(false)
const submitting = ref(false)

const selectedCustomer = computed(() =>
  customers.value.find((customer) => String(customer.id) === String(selectedCustomerId.value)),
)

const selectedProduct = computed(() =>
  products.value.find((product) => String(product.id) === String(selectedProductId.value)),
)

const lineTotal = computed(() => {
  if (!selectedProduct.value) return 0

  const discount = Math.max(0, Math.min(100, Number(discountPercent.value || 0)))
  const unitPrice = Number(selectedProduct.value.unitPrice || 0) * (1 - discount / 100)
  return unitPrice * Number(quantity.value || 0)
})

onMounted(loadCatalog)

async function loadCatalog() {
  loading.value = true

  try {
    const [customerValue, productValue] = await Promise.all([
      getOrderCustomers(),
      getOrderProducts(),
    ])

    customers.value = customerValue
    products.value = productValue
    selectedCustomerId.value = customers.value[0]?.id ?? ''
    selectedProductId.value = products.value[0]?.id ?? ''
  } catch {
    services.ui.showNotification('Falha ao carregar catálogo de encomendas', 'error')
  } finally {
    loading.value = false
  }
}

async function createOrder() {
  if (!selectedCustomerId.value || !selectedProductId.value) {
    services.ui.showNotification('Selecione cliente e produto', 'warning')
    return
  }

  submitting.value = true

  try {
    const payload = {
      customerId: selectedCustomerId.value,
      poNumber: null,
      notes: 'Criação rápida a partir do widget do dashboard',
      requestedDeliveryDate: null,
      paymentMethod: 'Fatura',
      shippingAddress: {
        street: 'Morada de expedição padrão',
        city: selectedCustomer.value?.city ?? '',
        postalCode: '',
        country: 'Portugal',
      },
      lines: [
        {
          productId: selectedProductId.value,
          quantity: Number(quantity.value || 1),
          discountPercent: Number(discountPercent.value || 0),
        },
      ],
    }

    const order = await createOrderApi(payload)

    services.eventBus.publish('orders:created', order)
    services.ui.showNotification(`Encomenda ${order.orderNumber} criada`, 'success')
  } catch {
    services.ui.showNotification('Não foi possível criar a encomenda', 'error')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="order-quick-create">
    <div v-if="loading" class="order-quick-create__loading">
      <span>Carregar catálogo...</span>
    </div>

    <template v-else>
      <div class="order-quick-create__intro">
        <span class="order-quick-create__badge">Vue SFC</span>
        <strong>Criar em 30 segundos</strong>
        <small>Consulta catálogo e submete a encomenda para a API do plugin.</small>
      </div>

      <div class="order-quick-create__form">
        <label class="order-quick-create__field">
          <span>Cliente</span>
          <select v-model="selectedCustomerId">
            <option
              v-for="customer in customers"
              :key="customer.id"
              :value="customer.id"
            >
              {{ customer.name }}
            </option>
          </select>
        </label>

        <label class="order-quick-create__field">
          <span>Produto</span>
          <select v-model="selectedProductId">
            <option
              v-for="product in products"
              :key="product.id"
              :value="product.id"
            >
              {{ product.sku }} — {{ product.name }}
            </option>
          </select>
        </label>

        <div class="order-quick-create__row">
          <label class="order-quick-create__field">
            <span>Quantidade</span>
            <input
              v-model.number="quantity"
              type="number"
              min="1"
              :max="selectedProduct?.stock || 999"
            >
          </label>

          <label class="order-quick-create__field">
            <span>Desconto %</span>
            <input
              v-model.number="discountPercent"
              type="number"
              min="0"
              max="100"
            >
          </label>
        </div>

        <div v-if="selectedProduct" class="order-quick-create__stock">
          Stock disponível: {{ selectedProduct.stock }} unidades
        </div>
      </div>

      <div class="order-quick-create__total">
        <span>Total estimado</span>
        <strong>{{ formatMoney(lineTotal, 'EUR') }}</strong>
      </div>

      <button
        class="order-quick-create__button"
        type="button"
        :disabled="submitting"
        @click="createOrder"
      >
        {{ submitting ? 'A criar...' : 'Criar encomenda' }}
      </button>
    </template>
  </div>
</template>

<style scoped>
.order-quick-create {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.order-quick-create__intro {
  display: grid;
  gap: 4px;
  padding: 12px;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 12px;
  background: linear-gradient(135deg, var(--el-color-primary-light-9), var(--el-fill-color));
}

.order-quick-create__intro strong {
  font-size: 16px;
  color: var(--el-text-color-primary);
}

.order-quick-create__intro small {
  color: var(--el-text-color-secondary);
}

.order-quick-create__badge {
  width: fit-content;
  padding: 3px 8px;
  border-radius: 999px;
  background: var(--el-color-primary);
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.2px;
}

.order-quick-create__loading {
  color: var(--el-text-color-secondary);
  font-size: 13px;
}

.order-quick-create__form {
  display: grid;
  gap: 12px;
}

.order-quick-create__field {
  display: grid;
  gap: 6px;
  color: var(--el-text-color-regular);
  font-size: 13px;
}

.order-quick-create__field select,
.order-quick-create__field input {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid var(--el-border-color);
  border-radius: 8px;
  padding: 8px 10px;
  background: var(--el-fill-color-blank);
  color: var(--el-text-color-primary);
  outline: none;
}

.order-quick-create__field select:focus,
.order-quick-create__field input:focus {
  border-color: var(--el-color-primary);
  box-shadow: 0 0 0 2px var(--el-color-primary-light-9);
}

.order-quick-create__row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.order-quick-create__stock {
  border-radius: 8px;
  padding: 8px 10px;
  background: var(--el-color-info-light-9);
  color: var(--el-color-info);
  font-size: 13px;
}

.order-quick-create__button {
  border: 0;
  border-radius: 8px;
  padding: 9px 12px;
  background: var(--el-color-primary);
  color: #fff;
  cursor: pointer;
  font-weight: 600;
}

.order-quick-create__button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.order-quick-create__total {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px;
  border-radius: 10px;
  background: var(--el-fill-color-light);
  color: var(--el-text-color-secondary);
}

.order-quick-create__total strong {
  color: var(--el-color-primary);
  font-size: 20px;
}
</style>
