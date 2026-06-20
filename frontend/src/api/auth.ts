import {
  getAdminShellHostV1,
  type LoginRequest,
  type RefreshTokenRequest,
  type RegisterRequest,
  type UserDto,
} from '@/generated/api/adminshell'
import type { User } from '@admin-shell/ui/types'

const api = getAdminShellHostV1()

export interface LoginPayload {
  email: string
  password: string
}

export interface LoginResult {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user?: User
}

export interface RegisterPayload {
  email: string
  username: string
  password: string
  displayName?: string
}

export async function login(
  email: string,
  password: string,
): Promise<LoginResult> {
  const response = await api.postApiV1AuthLogin({ email, password } satisfies LoginRequest)
  return response.data as LoginResult
}

export async function register(
  email: string,
  username: string,
  password: string,
  displayName?: string,
): Promise<LoginResult> {
  const response = await api.postApiV1AuthRegister({ email, username, password, displayName: displayName ?? null } satisfies RegisterRequest)
  return response.data as LoginResult
}

export async function refresh(
  accessToken: string,
  refreshToken: string,
): Promise<LoginResult> {
  const response = await api.postApiV1AuthRefresh({ accessToken, refreshToken } satisfies RefreshTokenRequest)
  return response.data as LoginResult
}

export async function logout(): Promise<void> {
  await api.postApiV1AuthLogout()
}

export async function getMe(): Promise<User> {
  const response = await api.getApiV1AuthMe()
  return response.data as UserDto as User
}
