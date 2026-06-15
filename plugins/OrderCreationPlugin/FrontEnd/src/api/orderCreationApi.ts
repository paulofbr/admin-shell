import {
  getOrderCreationPluginAPI,
  type CreateOrderRequest as GeneratedCreateOrderRequest,
  type CreateOrderResult,
} from '../generated/api'
import type { CustomerDto, CreateOrderRequest, OrderDto, OrderSummaryDto, OrdersPageResponse, ProductDto } from '../types'

const orderCreationApi = getOrderCreationPluginAPI()

function unwrapValue<T>(response: { data: { value: unknown } }): T {
  return response.data.value as T
}

export async function getOrderCustomers(): Promise<CustomerDto[]> {
  const response = await orderCreationApi.getOrderCustomers()
  return unwrapValue<CustomerDto[]>(response)
}

export async function getOrderProducts(): Promise<ProductDto[]> {
  const response = await orderCreationApi.getOrderProducts()
  return unwrapValue<ProductDto[]>(response)
}

export async function getOrders(page = 1, pageSize = 20): Promise<OrdersPageResponse> {
  const response = await orderCreationApi.getOrders({ page, pageSize })
  return unwrapValue<OrdersPageResponse>(response)
}

export async function getOrderSummary(): Promise<OrderSummaryDto> {
  const response = await orderCreationApi.getOrderSummary()
  return unwrapValue<OrderSummaryDto>(response)
}

export async function createOrderApi(request: CreateOrderRequest): Promise<OrderDto> {
  const response = await orderCreationApi.createOrder(toGeneratedRequest(request))
  return unwrapValue<OrderDto>(response)
}

function toGeneratedRequest(request: CreateOrderRequest): GeneratedCreateOrderRequest {
  return request as GeneratedCreateOrderRequest
}

export type { CreateOrderResult }
