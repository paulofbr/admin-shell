import client from '@/api/client'

export const authApi = {
  get: (url: string) => client.get(url),
  post: (url: string, data?: any) => client.post(url, data),
  put: (url: string, data?: any) => client.put(url, data),
  delete: (url: string) => client.delete(url),
}

export default client
