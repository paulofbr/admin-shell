import {
  getAdminShellHostV1,
  type MessageResponse,
  type SettingDto,
  type UpdateSettingRequest,
} from '@/generated/api/adminshell'

const api = getAdminShellHostV1()

export async function getSettings(): Promise<SettingDto[]> {
  const response = await api.getApiSettings()
  return response.data
}

export async function getSettingCategories(): Promise<string[]> {
  const response = await api.getApiSettingsCategories()
  return response.data
}

export async function saveSettings(changes: Record<string, string>): Promise<MessageResponse> {
  const payload = Object.entries(changes).map(([key, value]) => ({ key, value }) satisfies UpdateSettingRequest)
  const response = await api.putApiSettings(payload)
  return response.data
}
