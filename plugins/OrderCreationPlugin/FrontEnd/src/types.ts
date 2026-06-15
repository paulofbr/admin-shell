import { inject, type InjectionKey } from 'vue'
import type { AxiosResponse } from 'axios'

export interface PluginApiService {
  get: <T = unknown>(path: string, config?: Record<string, unknown>) => Promise<AxiosResponse<T>>
  post: <T = unknown, D = unknown>(path: string, data?: D, config?: Record<string, unknown>) => Promise<AxiosResponse<T>>
  put: <T = unknown, D = unknown>(path: string, data?: D, config?: Record<string, unknown>) => Promise<AxiosResponse<T>>
  patch: <T = unknown, D = unknown>(path: string, data?: D, config?: Record<string, unknown>) => Promise<AxiosResponse<T>>
  delete: <T = unknown>(path: string, config?: Record<string, unknown>) => Promise<AxiosResponse<T>>
}

export interface PluginEventBus {
  publish: <T>(event: string, data: T) => void
  subscribe: <T>(event: string, handler: (data: T) => void) => () => void
}

export interface PluginNavigationService {
  navigate: (path: string) => void
  getCurrentPath: () => string
}

export interface PluginNotificationService {
  showNotification: (message: string, type?: 'success' | 'error' | 'warning' | 'info') => void
}

export interface PluginComponentRegistry {
  register: (name: string, component: unknown) => void
  resolve: (name: string) => unknown
}

export interface PluginServices {
  api: PluginApiService
  eventBus: PluginEventBus
  navigation: PluginNavigationService
  ui: PluginNotificationService
  components: PluginComponentRegistry
}

export const pluginServicesKey: InjectionKey<PluginServices> = Symbol('OrderCreationPluginServices')

export function usePluginServices(pluginId?: string): PluginServices {
  const injectionKey = pluginId
    ? `admin-shell:plugin-services:${pluginId}`
    : pluginServicesKey

  const services = inject(injectionKey)

  if (!services) {
    throw new Error('OrderCreationPlugin services are not available')
  }

  return services
}

export interface WidgetDescriptor {
  id: string
  title: string
  zone: string
  width: number
  height: number
  order: number
  componentName?: string
  settings?: Record<string, unknown>
}

export interface CustomerDto {
  id: string
  name: string
  city?: string
}

export interface ProductDto {
  id: string
  sku: string
  name: string
  unitPrice: number
  stock: number
}

export interface CreateOrderLineRequest {
  productId: string
  quantity: number
  discountPercent: number
}

export interface CreateOrderRequest {
  customerId: string
  poNumber: string | null
  requestedDeliveryDate: string | null
  paymentMethod: string
  notes: string | null
  shippingAddress: {
    street: string
    city: string
    postalCode: string
    country: string
  }
  lines: CreateOrderLineRequest[]
}

export interface OrderLineDto {
  productId: string
  quantity: number
  discountPercent: number
  unitPrice: number
  lineTotal: number
}

export interface OrderDto {
  id: string
  orderNumber: string
  customerName: string
  status: string
  requestedDeliveryDate?: string | null
  total: number
  currency: string
  lines: OrderLineDto[]
}

export interface OrdersPageResponse {
  data: OrderDto[]
}

export interface OrderSummaryDto {
  openOrders: number
  openValue: number
  nextOrderNumber: string
  todayRevenue: number
  lowStockProducts: number
}

export function formatMoney(value: unknown, currency = 'EUR'): string {
  return new Intl.NumberFormat('pt-PT', {
    style: 'currency',
    currency,
  }).format(Number(value || 0))
}

export function formatDate(value: string | number | Date | null | undefined): string {
  if (!value) return '-'
  return new Date(value).toLocaleDateString('pt-PT')
}

export function unwrapData<T>(response: AxiosResponse<T | { data?: T }>): T | undefined {
  const payload = response.data
  if (payload && typeof payload === 'object' && 'data' in payload) {
    return (payload as { data?: T }).data
  }

  return payload as T
}
