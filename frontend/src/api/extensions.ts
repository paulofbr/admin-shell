import { getAdminShellHostV1, type ExtensionRegistrySnapshot } from '@/generated/api/adminshell'

const api = getAdminShellHostV1()

export async function getExtensions(): Promise<ExtensionRegistrySnapshot> {
  const response = await api.getApiV1Extensions()
  return response.data
}
