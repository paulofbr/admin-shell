import client from '@admin-shell/ui/http-client'

export const authApi = {
  get: (url: string, config?: any) => client.get(url, config),
  post: (url: string, data?: any, config?: any) => client.post(url, data, config),
  put: (url: string, data?: any, config?: any) => client.put(url, data, config),
  patch: (url: string, data?: any, config?: any) => client.patch(url, data, config),
  delete: (url: string, config?: any) => client.delete(url, config),
}

export default client
