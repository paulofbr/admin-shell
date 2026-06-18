import type { AxiosRequestConfig, AxiosResponse } from 'axios'
import { httpClient as baseHttpClient } from '../../../../../frontend/packages/admin-shell-ui/src/http-client/index'

export const httpClient = baseHttpClient as <T>(
  config: AxiosRequestConfig,
) => Promise<AxiosResponse<T>>
