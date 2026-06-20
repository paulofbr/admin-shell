import { getAdminShellHostV1, type DashboardMetricsResponse } from '@/generated/api/adminshell'

const api = getAdminShellHostV1()

export async function getDashboardMetrics(): Promise<DashboardMetricsResponse> {
  const response = await api.getApiV1DashboardMetrics()
  return response.data
}
