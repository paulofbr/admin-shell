import {
  getAdminShellHostV1,
  type PostApiV1PluginsInstallBody,
} from '@/generated/api/adminshell'
import type { HealthStatus, PluginInstallResult } from '@admin-shell/ui/types'

const api = getAdminShellHostV1()

export async function getHealth(): Promise<HealthStatus> {
  const response = await api.getApiV1Health()
  return response.data as unknown as HealthStatus
}

export async function installPlugin(file: File, activate = true): Promise<PluginInstallResult> {
  const response = await api.postApiV1PluginsInstall({ file, activate } satisfies PostApiV1PluginsInstallBody)
  return response.data as PluginInstallResult
}

export async function enablePlugin(id: string): Promise<void> {
  await api.postApiV1PluginsPluginIdEnable(id)
}

export async function disablePlugin(id: string): Promise<void> {
  await api.postApiV1PluginsPluginIdDisable(id)
}
