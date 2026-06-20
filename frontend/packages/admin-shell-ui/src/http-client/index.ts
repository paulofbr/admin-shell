import axios, {
  type AxiosError,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios'

const client = axios.create({
  baseURL: 'http://127.0.0.1:5000',
  headers: {
    'Content-Type': 'application/json',
  },
})

type QueuedRequest = {
  resolve: (token: string) => void
  reject: (error: AxiosError) => void
}

const TOKEN_KEY = 'adminshell-token'
const REFRESH_TOKEN_KEY = 'adminshell-refresh-token'
const LEGACY_TOKEN_KEY = 'auth_token'
const LEGACY_REFRESH_TOKEN_KEY = 'auth_refresh'

function getStoredToken(key: string): string | null {
  return localStorage.getItem(key)
}

function getAccessToken(): string | null {
  return getStoredToken(TOKEN_KEY) ?? getStoredToken(LEGACY_TOKEN_KEY)
}

function getRefreshToken(): string | null {
  return getStoredToken(REFRESH_TOKEN_KEY) ?? getStoredToken(LEGACY_REFRESH_TOKEN_KEY)
}

function setTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(TOKEN_KEY, accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  localStorage.setItem(LEGACY_TOKEN_KEY, accessToken)
  localStorage.setItem(LEGACY_REFRESH_TOKEN_KEY, refreshToken)
}

function clearTokens(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(LEGACY_TOKEN_KEY)
  localStorage.removeItem(LEGACY_REFRESH_TOKEN_KEY)
}

let isRefreshing = false
let failedQueue: QueuedRequest[] = []

function processQueue(error: AxiosError | null, token: string | null = null): void {
  failedQueue.forEach((queued) => {
    if (error) {
      queued.reject(error)
    } else {
      queued.resolve(token ?? '')
    }
  })

  failedQueue = []
}

client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken()

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

client.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    if (error.response?.status !== 401 || originalRequest._retry || !originalRequest.headers) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({
          resolve: (token: string) => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            resolve(client.request(originalRequest))
          },
          reject,
        })
      })
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const refreshToken = getRefreshToken()

      if (!refreshToken) {
        clearTokens()
        return Promise.reject(error)
      }

      const response = await client.post('/api/v1/Auth/refresh', { refreshToken })
      const { accessToken, refreshToken: nextRefreshToken } = response.data

      setTokens(accessToken, nextRefreshToken)

      originalRequest.headers.Authorization = `Bearer ${accessToken}`
      processQueue(null, accessToken)

      return client.request(originalRequest)
    } catch (refreshError) {
      processQueue(refreshError as AxiosError, null)
      clearTokens()
      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  },
)

export const httpClient = (async (config: Parameters<typeof client.request>[0]) => {
  const response = await client.request(config)
  return response
}) as <T>(
  config: Parameters<typeof client.request>[0],
) => Promise<AxiosResponse<T>>

export default client
