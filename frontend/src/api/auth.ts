import {
  getAdminShellHostV1,
  type LoginRequest,
  type RefreshTokenRequest,
  type RegisterRequest,
  type UserDto,
} from '@/generated/api/adminshell'
import type { User } from '@/types'

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
  const response = await api.postApiAuthLogin({ email, password } satisfies LoginRequest)
  return response.data as LoginResult
}

export async function register(
  email: string,
  username: string,
  password: string,
  displayName?: string,
): Promise<LoginResult> {
  const response = await api.postApiAuthRegister({ email, username, password, displayName: displayName ?? null } satisfies RegisterRequest)
  return response.data as LoginResult
}

export async function refresh(
  accessToken: string,
  refreshToken: string,
): Promise<LoginResult> {
  const response = await api.postApiAuthRefresh({ accessToken, refreshToken } satisfies RefreshTokenRequest)
  return response.data as LoginResult
}

export async function logout(): Promise<void> {
  await api.postApiAuthLogout()
}

export async function getMe(): Promise<User> {
  const response = await api.getApiAuthMe()
  return response.data as UserDto as User
}
