import {
  getAdminShellHostV1,
  type MessageResponse,
  type SettingDto,
  type UpdateSettingRequest,
} from '@/generated/api/adminshell'

const api = getAdminShellHostV1()

export async function getSettings(): Promise<SettingDto[]> {
  const response = await api.getApiV1Settings()
  return response.data
}

export async function getSettingCategories(): Promise<string[]> {
  const response = await api.getApiV1SettingsCategories()
  return response.data
}

export async function saveSettings(changes: Record<string, string>): Promise<MessageResponse> {
  const payload = Object.entries(changes).map(([key, value]) => ({ key, value }) satisfies UpdateSettingRequest)
  const response = await api.putApiV1Settings(payload)
  return response.data
}
