import { authApi } from '@/services/api'

export interface LogEntry {
  timestamp: string | null
  level: string
  source: string | null
  message: string
  exception: string | null
}

export interface LogPage {
  data: LogEntry[]
  hasMore: boolean
  scannedBytes: number
  warning: string | null
}

export async function getLogLevels(): Promise<string[]> {
  const response = await authApi.get('/api/v1/logs/levels')
  return response.data as string[]
}

export async function getLogs(params: {
  skip: number
  take: number
  type?: string
  message?: string
}): Promise<LogPage> {
  const response = await authApi.get('/api/v1/logs', {
    params: {
      skip: params.skip,
      take: params.take,
      type: params.type || undefined,
      message: params.message || undefined,
    },
  })

  return response.data
}
