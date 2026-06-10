import type { HealthStatus } from '@/types'
import client from './client'

export async function getHealth(): Promise<HealthStatus> {
  const response = await client.get<HealthStatus>('/api/health')
  return response.data
}

export async function enablePlugin(id: string): Promise<void> {
  await client.post(`/api/plugins/${id}/enable`)
}

export async function disablePlugin(id: string): Promise<void> {
  await client.post(`/api/plugins/${id}/disable`)
}