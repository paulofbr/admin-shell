import axios, {
  type AxiosError,
  type InternalAxiosRequestConfig,
} from 'axios'

const client = axios.create({
  baseURL: '',
  headers: {
    'Content-Type': 'application/json',
  },
})

client.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('auth_token')
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error: AxiosError) => Promise.reject(error),
)

export const httpClient = async <T>(config: Parameters<typeof client.request>[0]): Promise<Awaited<ReturnType<typeof client.request<T>>>> => {
  const response = await client.request<T>(config)
  return response
}

export default client
