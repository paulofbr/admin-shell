import {
  getAdminShellHostV1,
  type PostApiPluginsInstallBody,
} from '@/generated/api/adminshell'
import type { HealthStatus, PluginInstallResult } from '@/types'

const api = getAdminShellHostV1()

export async function getHealth(): Promise<HealthStatus> {
  const response = await api.getApiHealth()
  return response.data as unknown as HealthStatus
}

export async function installPlugin(file: File, activate = true): Promise<PluginInstallResult> {
  const response = await api.postApiPluginsInstall({ file, activate } satisfies PostApiPluginsInstallBody)
  return response.data as PluginInstallResult
}

export async function enablePlugin(id: string): Promise<void> {
  await api.postApiPluginsPluginIdEnable(id)
}

export async function disablePlugin(id: string): Promise<void> {
  await api.postApiPluginsPluginIdDisable(id)
}
